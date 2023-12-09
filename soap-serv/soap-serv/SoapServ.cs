using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using soap_serv.proxy;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace soap_serv
{
    class SoapServ : ISoapServ
    {

        //clé API pour OpenRouteService
        private string ORSkey = "5b3ce3597851110001cf62489b5bee9a060d4ce9b9985dff4e09ea21";
        private string ORSkeybackup = "5b3ce3597851110001cf6248eed530eaf3b449faa40397b2321d0769";
        private int ORSremaining = 2000;

        //liste des catégories d'adresse de OpenCageData que l'on veut éviter car trop imprécises
        private static List<string> categories = new List<string>() { "area", "place", "city", "country" };
        private static List<string> categoriesIn = new List<string>() { "place", };

        private const int SEUIL_CONFIDENCE = 7;

        private static ProxyCacheClient proxyCacheClient = new ProxyCacheClient();

        private static List<JCDContract> JCDcontracts = new List<JCDContract>();


        public SoapServ()
        {
            // récupération des contrats et stations depuis le proxy
            recupererContract();
        }

        /**
         * Depuis une adresse sous forme de string, renvoie une liste d'adresses correspondantes trouvées par OpenCageData
         * 
         ** @param adress : adress to search
         ** @return a list of address corresponding to the adress
         **/
        public List<Address> GetAddresses(string adress)
        {
            
            checkBackup();
            // build de l'url de la requête
            var result = proxyCacheClient.GetOCD(adress);
            List<Address> addresses = new List<Address>();

            // récupération de l'élément "results" de la réponse, élément contenant les informations sur les adresses
            var json = JsonDocument.Parse(result).RootElement.GetProperty("results");
            foreach (var item in json.EnumerateArray())
            {
                // traitement par catégorie
                var category = item.GetProperty("components").GetProperty("_category").ToString();

                // on ne souhaite pas récupérer les adresses trop imprécises
                if (categories.Contains(category)) continue;
                //on définit un seuil de confiance à 7 pour éviter les adresses trop imprécises
                if (item.GetProperty("confidence").GetInt32() < SEUIL_CONFIDENCE) continue;

                //composition de l'adresse
                Address address = new Address();
                address.country = item.GetProperty("components").GetProperty("country").ToString();
                address.formatted = item.GetProperty("formatted").ToString();
                //traite le cas où le numéro de maison n'est pas renseigné
                if (item.GetProperty("components").TryGetProperty("road", out var road))
                {
                    address.road = road.ToString();
                }
                else
                {
                    address.road = "";
                }

                //traite le cas où le numéro de maison n'est pas renseigné
                if (item.GetProperty("components").TryGetProperty("house_number", out var houseNumber))
                {
                    address.houseNumber = houseNumber.ToString();
                }
                else
                {
                    address.houseNumber = "-1";
                }

                // récupération des coordonnées GPS
                address.position = new Position();
                address.position.latitude = item.GetProperty("geometry").GetProperty("lat").GetDouble();
                address.position.longitude = item.GetProperty("geometry").GetProperty("lng").GetDouble();

                // on ajoute l'adresse à la liste si elle n'y est pas déjà 
                if (!addresses.Contains(address)) addresses.Add(address);
            }
            return addresses;
        }

        /** <Expose>
         ** Path de address start à address end
         ** @param start : adress de début
         ** @param end : adress de fin
         ** 
         ** @return Contract_AMQ_Trip : trajet correspondant au chemin entre les deux adresses
         ** 
         **/
        public Contract_AMQ_Trip getPath(Address start, Address end)
        {
            Contract_AMQ_Trip contract = new Contract_AMQ_Trip();
            Trip startToStation = getBestTripToStation(start, true);
            Trip stationToEnd = getBestTripToStation(end, false);
            Trip footwalking = getTrip(start.position, end.position, TypeOfTrip.foot_walking).Result;
            // station de départ et station d'arrivée sont les mêmes
            if (startToStation.directions.ElementAt(startToStation.directions.Count() - 1).end.Equals(stationToEnd.directions.ElementAt(0).start))
            {
                contract.allTrip.Add(footwalking);
            }
            // station de départ et station d'arrivée sont différentes
            else
            {
                // trajet entre les deux stations
                Task<Trip> stationToStation = getTrip(startToStation.directions.ElementAt(startToStation.directions.Count() - 1).end, stationToEnd.directions.ElementAt(0).start, TypeOfTrip.cycling_regular);

                contract.allTrip.Add(startToStation);

                stationToStation.Wait();
                contract.allTrip.Add(stationToStation.Result);

                contract.allTrip.Add(stationToEnd);
                // si le trajet à pied est plus rapide que le trajet en vélo, on ne prend que le trajet à pied
                if (footwalking.duration < contract.allTrip.AsQueryable().Sum(x => x.duration))
                {
                    contract.allTrip.Clear();
                    contract.allTrip.Add(footwalking);
                }

            }
            
            // ACTIVEMQ
            try
            {
                // Create a Connection Factory.
                Uri connecturi = new Uri("activemq:tcp://localhost:64445");
                ConnectionFactory connectionFactory = new ConnectionFactory(connecturi);

                // Create a single Connection from the Connection Factory.
                IConnection connection = connectionFactory.CreateConnection();
                connection.Start();

                // Create a session from the Connection.
                ISession session = connection.CreateSession();

                string clientqueue = "queueItinéraire";
                // Use the session to target a queue.
                IDestination destination = session.GetQueue(clientqueue);

                // Create a Producer targetting the selected queue.
                IMessageProducer producer = session.CreateProducer(destination);

                // You may configure everything to your needs, for instance:
                producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

                // pour chaque trajet
                foreach (Trip trip in contract.allTrip)
                {
                    // pour chaque direction
                    foreach (Direction direction in trip.directions)
                    {
                        // envoie de la directive
                        string message = direction.direction;
                        ITextMessage textMessage = session.CreateTextMessage(message);
                        producer.Send(textMessage);
                    }
                }

                // Don't forget to close your session and connection when finished.
                session.Close();
                connection.Close();

                contract.nameQueue = "queueItinéraire";
                contract.isQueue = true;

            }
            catch (Exception e)
            {
                contract.isQueue = false;
            }

            return contract;
        }


        /** <Interne>
         ** Depuis une adresse start et une adresse end, renvoie un trajet correspondant au chemin entre les deux adresses
         *
         ** Methode de traitement de requete OpenRouteService
         *
         ** @param start : adress de début
         ** @param end : adress de fin
         ** @return Trip : trajet correspondant au chemin entre les deux adresses
         **/
        public async Task<Trip> getTrip(Position start, Position end, TypeOfTrip type)
        {

            // format Double en anglais pour OpenRouteService
            CultureInfo enform = new CultureInfo("en-US");
            Trip trip = new Trip(type);

            string strtype = (trip.type).ToString().Replace("_", "-");

            // build de l'url de la requête
            string url = "https://api.openrouteservice.org/v2/directions/" + strtype + "?api_key=" + ORSkey + "&start=";
            url += start.longitude.ToString(enform) + "," + start.latitude.ToString(enform);
            url += "&end=";
            url += end.longitude.ToString(enform) + "," + end.latitude.ToString(enform);

            // envoie de la requête 
            HttpClient client = new HttpClient();
            var response = client.GetAsync(url).Result;
            var result = response.Content.ReadAsStringAsync().Result;


            // traitement de la réponse
            if (response.IsSuccessStatusCode)
            {

                // selonnle nombre de requêtes restantes, on change de clé API
                response.Headers.TryGetValues("x-ratelimit-remaining", out IEnumerable<string> values);
                ORSremaining = int.Parse(values.ElementAt(0));
                checkBackup();

                // récupération de l'élément "features" de la réponse, élément contenant les informations sur le trajet
                var data = JsonDocument.Parse(result).RootElement.GetProperty("features").EnumerateArray().First();

                // récupération de tous les points du trajet
                var way_points = data.GetProperty("geometry").GetProperty("coordinates").EnumerateArray();

                // récupération de la durée et de la distance du trajet
                var duration = data.GetProperty("properties").GetProperty("summary").GetProperty("duration").GetDouble();
                var distance = data.GetProperty("properties").GetProperty("summary").GetProperty("distance").GetDouble();


                List<Direction> directions = new List<Direction>();

                // récupération des étapes du trajet, avec les informations sur la direction, la durée et la distance
                var steps = data.GetProperty("properties").GetProperty("segments").EnumerateArray().First().GetProperty("steps").EnumerateArray();

                // traitement de chaque étape
                foreach (var item in steps)
                {
                    Direction direction = new Direction();

                    // récupération des informations sur l'étape
                    direction.direction = item.GetProperty("instruction").ToString();
                    direction.duration = item.GetProperty("duration").GetDouble();
                    direction.distance = item.GetProperty("distance").GetDouble();

                    // récupération des points de l'étape
                    var s_wp = item.GetProperty("way_points").EnumerateArray();

                    List<Position> positions = new List<Position>();

                    // traitement de chaque point de l'étape
                    // on va récupérer les points d'étapes renseignés correspondant à l'étape courrante
                    for (int i = s_wp.ElementAt(0).GetInt32(); i <= s_wp.ElementAt(1).GetInt32(); i++)
                    {
                        Position p = new Position();

                        var coo = way_points.ElementAt(i).EnumerateArray();

                        // récupération des coordonnées GPS du point dans la partie "way_points" de la réponse
                        p.longitude = coo.ElementAt(0).GetDouble();
                        p.latitude = coo.ElementAt(1).GetDouble();

                        positions.Add(p);
                    }

                    // ajout des points de l'étape à la direction
                    direction.way_points = positions;

                    // définition des points de départ et d'arrivée de la direction
                    direction.start = positions.ElementAt(0);
                    direction.end = positions.ElementAt(positions.Count - 1);

                    directions.Add(direction);
                }

                trip.directions = directions;
                trip.duration = duration;
                trip.distance = distance;

            }
            else
            {
                throw new Exception("Error while getting path : statut code - " + response.StatusCode);
            }

            return trip;
        }

        /** <Interne>
         * Backup 
         */
        public void checkBackup()
        {
            if (ORSremaining < 100)
            {
                ORSkey = ORSkeybackup;
            }
        }


        /** <Interne>
         ** Depuis un nom de ville, recuperer la position GPS de la ville
         *
         ** @param cityname : nom de la ville
         ** @return Position : position GPS de la ville
         **/
        public Address GetVille(String cityname)
        {

            var result = proxyCacheClient.GetOCD(cityname);

            // récupération de l'élément "results" de la réponse, élément contenant les informations sur les adresses
            var json = JsonDocument.Parse(result).RootElement.GetProperty("results");

            Address address = new Address();
            foreach (var item in json.EnumerateArray())
            {
                // traitement par catégorie
                var category = item.GetProperty("components").GetProperty("_category").ToString();

                // on ne souhaite pas récupérer les adresses trop imprécises
                if (categoriesIn.Contains(category))
                {
                    address.city = cityname;
                    address.formatted = item.GetProperty("formatted").ToString();
                    address.road = "";
                    address.houseNumber = "";
                    // récupération des coordonnées GPS
                    address.position = new Position();
                    address.position.latitude = item.GetProperty("geometry").GetProperty("lat").GetDouble();
                    address.position.longitude = item.GetProperty("geometry").GetProperty("lng").GetDouble();
                    return address;
                }
            }
            return null;
        }


        /** <Interne>
         ** Computer toutes les contrats
         *
         ** Gestion des contrats sans stations
         **/
        public void recupererContract()
        {
            // recuperation des contrats
            string contracts = proxyCacheClient.Get("all-contract");
            JsonElement resp = JsonDocument.Parse(contracts).RootElement;
            Task<JCDContract>[] tasks = new Task<JCDContract>[resp.GetArrayLength()];
            int indextask = 0;
            // pour chaque contrat, on lance un thread pour le construire
            foreach (JsonElement jo in resp.EnumerateArray())
            {
                tasks[indextask] = Task.Run(() => buildContract(jo));
                indextask++;
            }

            Task.WaitAll(tasks);

            // traitement des resultats (gestion des contrats sans stations)
            for(int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].Result != null)
                {
                    var json = proxyCacheClient.Get(tasks[i].Result.name);
                    // avec stations
                    if (!JsonDocument.Parse(json).RootElement.EnumerateArray().Count().Equals(0))
                    {
                        JCDcontracts.Add(tasks[i].Result); }
                }
            }

        }


        /** <Interne>
         ** Depuis un JsonElement, construit un contrat
         *
         ** @param jo : JsonElement correspondant à un contrat
         ** @return JCDContract : contrat construit
         **/
        public async Task<JCDContract> buildContract(JsonElement jo)
        {
            JCDContract contract = new JCDContract();
            contract.name = jo.GetProperty("name").ToString();
            if (jo.TryGetProperty("cities", out var cities) && cities.ValueKind == JsonValueKind.Array)
            {
                Task<cityContract>[] tasksin = new Task<cityContract>[cities.GetArrayLength()];
                int ins = 0;
                foreach (JsonElement jv in cities.EnumerateArray())
                {

                    string name = jv.GetString();
                    //if (contract.name.Equals("cergy-pontoise"))
                    //{
                    tasksin[ins] = Task.Run(() => new cityContract() { name = name, position = GetVille(name).position });
                    ins++;
                    //}

                }
                Task.WaitAll(tasksin);

                // on ajoute les villes au contrat
                foreach (Task<cityContract> task in tasksin)
                {
                    contract.cities.Add(task.Result);
                }

                return contract;
            }
            else return null;
        }


        /** <Interne>
         ** Build de la liste des VecteurStations, d'une adresse vers les stations données
         *
         ** @param stations : liste des stations
         ** @param ad : adresse
         ** @return List<VecteurStation> : liste des VecteurStation
         **/
        public List<VecteurStation> distanceStation(List<JCDStation> stations, Address ad)
        {
            List<VecteurStation> vecteurStations = new List<VecteurStation>();
            foreach (JCDStation s in stations)
            {
                Vecteur vecteur = new Vecteur(ad.position, s.position);
                VecteurStation vecteurStation = new VecteurStation(s, vecteur);
                vecteurStations.Add(vecteurStation);
            }
            vecteurStations.Sort((vecteurStation1, vecteurStation2) => vecteurStation1.vecteur.GetTaille().CompareTo(vecteurStation2.vecteur.GetTaille()));
            return vecteurStations;
        }

        /** <Interne>
         ** Recuperation des stations d'un contrat
         *
         ** @param jCDContract : contrat
         ** @return List<JCDStation> : liste des stations du contrat
         **/
        public List<JCDStation> getStations(JCDContract jCDContract)
        {
            // recuperation des stations du contrat
            string response = proxyCacheClient.Get(jCDContract.name);

            // traitement de la réponse
            var result = JsonDocument.Parse(response).RootElement.EnumerateArray();

            List<JCDStation> stationsContract = new List<JCDStation>();

            // traitement pour chaque station
            for (int i =0; i<result.Count(); i++)
            {
                //construction de la station JCDStation
                JCDStation station = new JCDStation();
                station.name = result.ElementAt(i).GetProperty("name").ToString();
                station.position = new Position();
                station.position.latitude = result.ElementAt(i).GetProperty("position").GetProperty("latitude").GetDouble();
                station.position.longitude = result.ElementAt(i).GetProperty("position").GetProperty("longitude").GetDouble();

                station.bikes = result.ElementAt(i).GetProperty("totalStands").GetProperty("availabilities").GetProperty("bikes").GetInt32();
                station.stands = result.ElementAt(i).GetProperty("totalStands").GetProperty("availabilities").GetProperty("stands").GetInt32();
                station.capacity = result.ElementAt(i).GetProperty("totalStands").GetProperty("capacity").GetInt32();

                stationsContract.Add(station);
            }

            return stationsContract;

        }

        /** <Interne>
         ** Recuperation des trajet vers les stations les plus proches depuis une adresse
         *
         ** @param vecteurStations : liste des VecteurStation
         ** @param ad : adresse
         ** @param nbMax : nombre de trajet à recuperer
         ** @param indirectionstation : true si on veut aller vers la station, false si on veut partir de la station
         ** @return List<TripStation> : liste des TripStation
         **/
        public List<TripStation> getListeStation(List<VecteurStation> vecteurStations, Address ad, int nbMax, bool indirectionstation)
        {
            // en cas de nombre de stations inférieur au nombre de trajet à recuperer
            if (vecteurStations.Count < nbMax)
            {
                nbMax = vecteurStations.Count;
            }

            List<TripStation> trips = new List<TripStation>();

            Task<Trip>[] tasks = new Task<Trip>[nbMax];

            // pour chaque station, on lance un thread pour recuperer le trajet vers la station
            for (int i = 0; i < nbMax; i++)
            {
                TripStation tripSt = new TripStation();
                tripSt = new TripStation() {station = vecteurStations.ElementAt(i).JCDStation };
                VecteurStation tc = vecteurStations.ElementAt(i);
                if (indirectionstation)
                {
                    tasks[i] = Task.Run(() => getTrip(ad.position, tc.JCDStation.position, TypeOfTrip.foot_walking));
                }
                else
                {
                    tasks[i] = Task.Run(() => getTrip(tc.JCDStation.position, ad.position, TypeOfTrip.foot_walking));
                }

                trips.Add(tripSt);
            }

            Task.WaitAll(tasks);

            // recuperation des resultats
            for (int i = 0; i < nbMax; i++)
            {
                trips.ElementAt(i).trip = tasks[i].Result;
            }

            // tri des trajets par durée
            trips.Sort((Trip1, Trip2) => Trip1.trip.duration.CompareTo(Trip2.trip.duration));

            return trips;
        }

        /** <Interne>
         ** Recuperation du meilleur trajet vers une station depuis une adresse
         *
         ** @param adr : adresse
         ** @param indirectionstation : true si on veut aller vers la station, false si on veut partir de la station
         **/
        public Trip getBestTripToStation(Address adr, bool indirectionstation)
        {


 //////////////////////
 ///                         Selections du contrat le plus proche de l'adresse en fonction de sa premiere ville
 ///                         
 ///                         Pour reduire le nombre de contrat à traiter et de ville à traiter
 ///                         3 plus proche à vol d'oiseau
 //////////////////////

            List<VecteurContrat> vecteurContrats = new List<VecteurContrat>();

            List<JCDContract> selectedContract = new List<JCDContract>();

            // construction de la liste des VecteurContrat
            foreach(JCDContract j in JCDcontracts)
            {
                if(j.cities != null && j.cities.Count > 0)
                {
                    Vecteur vecteur = new Vecteur(adr.position, j.cities.ElementAt(0).position);
                    VecteurContrat vecteurContrat = new VecteurContrat(j, vecteur, j.cities.ElementAt(0));
                    vecteurContrats.Add(vecteurContrat);
                }
            }

            // def max de contrat à traiter
            int max = 3;

            if (vecteurContrats.Count < max)
            {
                max = vecteurContrats.Count;
            }

            // tri des VecteurContrat par taille du vecteur
            vecteurContrats.Sort((vecteurContrat1, vecteurContrat2) => vecteurContrat1.vecteur.GetTaille().CompareTo(vecteurContrat2.vecteur.GetTaille()));

            // recuperation des max premiers
            vecteurContrats = vecteurContrats.GetRange(0, max);

            // recuperation des contrats
            selectedContract = vecteurContrats.Select(vecteurContrat => vecteurContrat.JCDContract).ToList();

            vecteurContrats = new List<VecteurContrat>();

//////////////////////
///                         Selections du contrat le plus proche de l'adresse en fonction de la ville la plus proche de l'adresse
///                         
///                         Pour verifier si le contrat dont la premiere ville du contrat est bien le meilleur contrat a prendre
///                         
///                         3 plus proches villes à vol d'oiseau
///                         --> au cas ou le centre du contrat est plus loin de la ville la plus proche de l'adresse
///                         
///                         le meilleur chemin (plus rapide) à pied vers une ville depuis les 3 plus proches
//////////////////////

            // construction de la liste des VecteurContrat en fonction de toutes les villes des 3 contrats selectionnés
            foreach (JCDContract j in selectedContract)
            {
                foreach (cityContract c in j.cities)
                {
                    Vecteur vecteur = new Vecteur(adr.position, c.position);
                    VecteurContrat vecteurContrat = new VecteurContrat(j, vecteur, c);
                    vecteurContrats.Add(vecteurContrat);
                }
            }

            max = 3;

            // tri des VecteurContrat par taille du vecteur
            vecteurContrats.Sort((vecteurContrat1, vecteurContrat2) => vecteurContrat1.vecteur.GetTaille().CompareTo(vecteurContrat2.vecteur.GetTaille()));

            if(vecteurContrats.Count < max)
            {
                max = vecteurContrats.Count;
            }

            // recuperation des max premiers
            vecteurContrats = vecteurContrats.GetRange(0, max);

            int nbMax = 3;

            // recuperation du meilleur trajet vers une station depuis une adresse depuis les vecteurContrats selectionnés
            TripContrat tripContrat = getContrat(vecteurContrats, nbMax, adr, indirectionstation);

            // recuperation des stations du contrat
            List<JCDStation> stations = getStations(tripContrat.contract);

            // suppression des stations sans vélos ou sans places selon le sens du trajet
            if(indirectionstation)
                stations.RemoveAll(x => x.bikes == 0);
            else 
                stations.RemoveAll(x => x.stands == 0);

//////////////////////
///                         Selections des stations les plus proches de l'adresse
///                         
///                         Pour reduire le nombre de station à traiter
///                         4 plus proche à vol d'oiseau
///                         
///                         Recuperation de la station la plus proche
//////////////////////

            List<VecteurStation> vecteurStations = distanceStation(stations, adr);

            int nbMaxStations = 4;

            // recuperation des trajet des max premières stations
            List<TripStation> tripStations = getListeStation(vecteurStations, adr, nbMaxStations, indirectionstation);

            // meilleur trajet vers une station depuis une adresse
            return tripStations.ElementAt(0).trip;

        }


        /** <Interne>
         ** Recuperer le meilleur trajet vers un contrat depuis une adresse
         *
         ** @param vc : liste des VecteurContrat
         ** @param nbMax : nombre de trajet à recuperer
         ** @param ad : adresse
         ** @param indirectionstation : true si on veut aller vers la station, false si on veut partir de la station
         ** @return TripContrat : meilleur trajet vers un contrat depuis une adresse
         **/
        public TripContrat getContrat(List<VecteurContrat> vc, int nbMax, Address ad, bool indirectionstation)
        {
            if (vc.Count < nbMax)
            {
                nbMax = vc.Count;
            }

            List<TripContrat> tripContrats = new List<TripContrat>();

            Task<Trip>[] tasks = new Task<Trip>[nbMax];
            // pour chaque contrat, on lance un thread pour recuperer le trajet vers le contrat
            for (int i = 0; i < nbMax; i++)
            {
                TripStation tripSt = new TripStation();
                TripContrat tripContrat = new TripContrat() { contract = vc.ElementAt(i).JCDContract };
                VecteurContrat tc = vc.ElementAt(i);
                if (indirectionstation)
                {
                    tasks[i] = Task.Run(() => getTrip(ad.position, tc.cityContract.position, TypeOfTrip.foot_walking));
                }
                else
                {
                    tasks[i] = Task.Run(() => getTrip(tc.cityContract.position, ad.position, TypeOfTrip.foot_walking));
                }

                tripContrats.Add(tripContrat);
            }

            Task.WaitAll(tasks);

            // recuperation des resultats
            for (int i = 0; i < nbMax; i++)
            {
                tripContrats.ElementAt(i).trip = tasks[i].Result;
            }
            tasks = new Task<Trip>[nbMax];

            // tri des trajets par durée
            tripContrats.Sort((TripContrat1, TripContrat2) => TripContrat1.trip.duration.CompareTo(TripContrat2.trip.duration));

            // recuperation du meilleur trajet vers un contrat depuis une adresse
            return tripContrats.ElementAt(0);
        }
    }
}
