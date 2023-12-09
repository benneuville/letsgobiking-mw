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

        //clé API pour OpenCageData
        private string OCDkey = "4bd5b96da6bf40eebc28429db33e2089";
        private string OCDkeybackup = "3b468aae7c184bdea7b240ea301d2814";
        private int OCDremaining = 2500;

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
            OCDremaining = JsonDocument.Parse(result).RootElement.GetProperty("rate").GetProperty("remaining").GetInt32();
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

        public Contract_AMQ_Trip getPath(Address start, Address end)
        {
            Contract_AMQ_Trip contract = new Contract_AMQ_Trip();
            Trip startToStation = getBestTripToStation(start, true);
            Trip stationToEnd = getBestTripToStation(end, false);
            Trip footwalking = getTrip(start.position, end.position, TypeOfTrip.foot_walking).Result;
            if (startToStation.directions.ElementAt(startToStation.directions.Count() - 1).end.Equals(stationToEnd.directions.ElementAt(0).start))
            {
                contract.allTrip.Add(footwalking);
            }
            else
            {
                contract.allTrip.Add(startToStation);
                Trip stationToStation = getTrip(startToStation.directions.ElementAt(startToStation.directions.Count() - 1).end, stationToEnd.directions.ElementAt(0).start, TypeOfTrip.cycling_regular).Result;


                contract.allTrip.Add(stationToStation);
                contract.allTrip.Add(stationToEnd);
                if (footwalking.duration < contract.allTrip.AsQueryable().Sum(x => x.duration))
                {
                    contract.allTrip.Clear();
                    contract.allTrip.Add(footwalking);
                }

            }
            
            try
            {
                // Create a Connection Factory.
                Uri connecturi = new Uri("activemq:tcp://localhost:6465");
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

                foreach (Trip trip in contract.allTrip)
                {
                    foreach (Direction direction in trip.directions)
                    {
                        string message = direction.direction;
                        ITextMessage textMessage = session.CreateTextMessage(message);
                        producer.Send(textMessage);
                    }
                    producer.Send(session.CreateTextMessage(" "));
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

        /**
         * Depuis une adresse start et une adresse end, renvoie un trajet correspondant au chemin entre les deux adresses
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

        public void checkBackup()
        {
            if (ORSremaining < 100)
            {
                ORSkey = ORSkeybackup;
            }
        }

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

        public void recupererContract()
        {
            string contracts = proxyCacheClient.Get("all-contract");
            JsonElement resp = JsonDocument.Parse(contracts).RootElement;
            Task<JCDContract>[] tasks = new Task<JCDContract>[resp.GetArrayLength()];
            int indextask = 0;
            foreach (JsonElement jo in resp.EnumerateArray())
            {
                tasks[indextask] = Task.Run(() => buildContract(jo));
                indextask++;
            }

            Task.WaitAll(tasks);

            for(int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].Result != null)
                {
                    var json = proxyCacheClient.Get(tasks[i].Result.name);
                    if (!JsonDocument.Parse(json).RootElement.EnumerateArray().Count().Equals(0))
                    {
                        JCDcontracts.Add(tasks[i].Result); }
                }
            }

        }

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

                foreach (Task<cityContract> task in tasksin)
                {
                    contract.cities.Add(task.Result);
                }

                return contract;
            }
            else return null;
        }


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

        public List<JCDStation> getStations(JCDContract jCDContract)
        {
            string response = proxyCacheClient.Get(jCDContract.name);

            var result = JsonDocument.Parse(response).RootElement.EnumerateArray();

            List<JCDStation> stationsContract = new List<JCDStation>();

            for (int i =0; i<result.Count(); i++)
            {
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

        public List<TripStation> getListeStation(List<VecteurStation> vecteurStations, Address ad, int nbMax, bool indirectionstation)
        {
            if (vecteurStations.Count < nbMax)
            {
                nbMax = vecteurStations.Count;
            }

            List<TripStation> trips = new List<TripStation>();

            Task<Trip>[] tasks = new Task<Trip>[nbMax];
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

            for (int i = 0; i < nbMax; i++)
            {
                trips.ElementAt(i).trip = tasks[i].Result;
            }

            trips.Sort((Trip1, Trip2) => Trip1.trip.duration.CompareTo(Trip2.trip.duration));

            return trips;
        }
        public Trip getBestTripToStation(Address adr, bool indirectionstation)
        {
            
            List<VecteurContrat> vecteurContrats = new List<VecteurContrat>();

            List<JCDContract> selectedContract = new List<JCDContract>();
            foreach(JCDContract j in JCDcontracts)
            {
                if(j.cities != null && j.cities.Count > 0)
                {
                    Vecteur vecteur = new Vecteur(adr.position, j.cities.ElementAt(0).position);
                    VecteurContrat vecteurContrat = new VecteurContrat(j, vecteur, j.cities.ElementAt(0));
                    vecteurContrats.Add(vecteurContrat);
                }
            }

            int max = 3;

            if (vecteurContrats.Count < max)
            {
                max = vecteurContrats.Count;
            }

            vecteurContrats.Sort((vecteurContrat1, vecteurContrat2) => vecteurContrat1.vecteur.GetTaille().CompareTo(vecteurContrat2.vecteur.GetTaille()));

            vecteurContrats = vecteurContrats.GetRange(0, max);

            selectedContract = vecteurContrats.Select(vecteurContrat => vecteurContrat.JCDContract).ToList();

            vecteurContrats = new List<VecteurContrat>();

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

            vecteurContrats.Sort((vecteurContrat1, vecteurContrat2) => vecteurContrat1.vecteur.GetTaille().CompareTo(vecteurContrat2.vecteur.GetTaille()));

            if(vecteurContrats.Count < max)
            {
                max = vecteurContrats.Count;
            }


            vecteurContrats = vecteurContrats.GetRange(0, max);

            int nbMax = 3;

            TripContrat tripContrat = getContrat(vecteurContrats, nbMax, adr, indirectionstation);

            List<JCDStation> stations = getStations(tripContrat.contract);

            if(indirectionstation)
                stations.RemoveAll(x => x.bikes == 0);
            else 
                stations.RemoveAll(x => x.stands == 0);

            List<VecteurStation> vecteurStations = distanceStation(stations, adr);

            int nbMaxStations = 4;

            List<TripStation> tripStations = getListeStation(vecteurStations, adr, nbMaxStations, indirectionstation);

            return tripStations.ElementAt(0).trip;

        }
        public TripContrat getContrat(List<VecteurContrat> vc, int nbMax, Address ad, bool indirectionstation)
        {
            if (vc.Count < nbMax)
            {
                nbMax = vc.Count;
            }

            List<TripContrat> tripContrats = new List<TripContrat>();

            Task<Trip>[] tasks = new Task<Trip>[nbMax];
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

            for (int i = 0; i < nbMax; i++)
            {
                tripContrats.ElementAt(i).trip = tasks[i].Result;
            }
            tasks = new Task<Trip>[nbMax];

            tripContrats.Sort((TripContrat1, TripContrat2) => TripContrat1.trip.duration.CompareTo(TripContrat2.trip.duration));

            TripContrat choice = tripContrats.ElementAt(0);

            foreach (TripContrat tc in tripContrats)
            {
                JsonElement json = JsonDocument.Parse(proxyCacheClient.Get(tc.contract.name)).RootElement;

                if(json.EnumerateArray().Count() != 0)
                {
                    choice = tc;
                }
            }

            return choice;
        }
    }
}
