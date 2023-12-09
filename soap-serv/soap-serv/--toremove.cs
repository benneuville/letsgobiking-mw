/*using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace soap_serv
{
    public static void TestVecteur()
    {
        Position depart = new Position();
        depart.latitude = 49.033333;
        depart.longitude = 2.066667;
        Position arrivee = new Position();
        arrivee.latitude = 52.28934;
        arrivee.longitude = 4.76827;

        Vecteur vecteur = new Vecteur(depart, arrivee);
        Console.WriteLine(vecteur.GetTaille());
        Console.WriteLine(vecteur.X);
        Console.WriteLine(vecteur.Y);


    }

    static public void getLesStationsLesPlusProches()
    {
        string adresseDepart = "Avenue des 3 Fontaines, 95000 Cergy";
        string adresseArrivee = "6 Av. de l'Île de France, 95300 Pontoise";

        Address depart = GetVille(adresseDepart);
        Address arrivee = GetVille(adresseArrivee);

        List<JCDContract> allContracts = appelProxy();

        List<VecteurContrat> vecteurContratsDepart = new List<VecteurContrat>();

        foreach (JCDContract j in allContracts)
        {
            foreach (cityContract c in j.cityContract)
            {
                Vecteur vecteur = new Vecteur(depart.position, c.position);
                VecteurContrat vecteurContrat = new VecteurContrat(j, vecteur, c);
                vecteurContratsDepart.Add(vecteurContrat);
            }
        }

        vecteurContratsDepart.Sort((vecteurContrat1, vecteurContrat2) => vecteurContrat1.vecteur.GetTaille().CompareTo(vecteurContrat2.vecteur.GetTaille()));

        List<VecteurContrat> vecteurContratsArrivee = new List<VecteurContrat>();

        foreach (JCDContract j in allContracts)
        {
            foreach (cityContract c in j.cityContract)
            {
                Vecteur vecteur = new Vecteur(arrivee.position, c.position);
                VecteurContrat vecteurContrat = new VecteurContrat(j, vecteur, c);
                vecteurContratsArrivee.Add(vecteurContrat);
            }
        }

        vecteurContratsArrivee.Sort((vecteurContrat1, vecteurContrat2) => vecteurContrat1.vecteur.GetTaille().CompareTo(vecteurContrat2.vecteur.GetTaille()));

        int nbMaxDepart = 4;

        TripContrat tripContratDepart = getContrat(vecteurContratsDepart, nbMaxDepart, depart);

        int nbMaxArrivee = 4;

        TripContrat tripContratArrivee = getContrat(vecteurContratsArrivee, nbMaxArrivee, arrivee);

        List<JCDStation> stationsDepart = getStations(tripContratDepart.contract);

        List<VecteurStation> vecteurStationsDepart = distanceStation(stationsDepart, depart);

        List<JCDStation> stationsArrivee = getStations(tripContratArrivee.contract);

        List<VecteurStation> vecteurStationsArrivee = distanceStation(stationsArrivee, arrivee);

        int nbMaxStations = 5;

        List<TripStation> tripStationsDepart = getListeStation(vecteurStationsDepart, depart, nbMaxStations);

        List<TripStation> tripStationsArrivee = getListeStation(vecteurStationsArrivee, arrivee, nbMaxStations);

        Console.WriteLine("Station la plus proche du départ : " + tripStationsDepart.ElementAt(0));
        Console.WriteLine("Station la plus proche de l'arrivée : " + tripStationsArrivee.ElementAt(0));

    }

    static public List<TripStation> getListeStation(List<VecteurStation> vecteurStations, Address ad, int nbMax)
    {
        if (vecteurStations.Count < nbMax)
        {
            nbMax = vecteurStations.Count;
        }

        List<TripStation> trips = new List<TripStation>();

        for (int i = 0; i < nbMax; i++)
        {
            TripStation tripSt = new TripStation();
            tripSt = new TripStation() { trip = getTripToStation(vecteurStations.ElementAt(i).JCDStation, ad), station = vecteurStations.ElementAt(i).JCDStation };
            trips.Add(tripSt);
        }

        trips.Sort((Trip1, Trip2) => Trip1.trip.duration.CompareTo(Trip2.trip.duration));

        return trips;
    }

    static public Trip getTripToStation(JCDStation jCDStation, Address ad)
    {
        Console.WriteLine("Path calculation");

        CultureInfo enform = new CultureInfo("en-US");
        Trip trip = new Trip();

        string url = "https://api.openrouteservice.org/v2/directions/foot-walking?api_key=" + ORSkey + "&start=";
        url += ad.position.longitude + "," + ad.position.latitude;
        url += "&end=";
        url += jCDStation.position.longitude + "," + jCDStation.position.latitude;

        HttpClient client = new HttpClient();
        var response = client.GetAsync(url).Result;
        var result = response.Content.ReadAsStringAsync().Result;

        if (response.IsSuccessStatusCode)
        {
            var data = JsonDocument.Parse(result).RootElement.GetProperty("features").EnumerateArray().First();

            var way_points = data.GetProperty("geometry").GetProperty("coordinates").EnumerateArray();

            var duration = data.GetProperty("properties").GetProperty("summary").GetProperty("duration").GetDouble();
            var distance = data.GetProperty("properties").GetProperty("summary").GetProperty("distance").GetDouble();

            List<Direction> directions = new List<Direction>();

            var steps = data.GetProperty("properties").GetProperty("segments").EnumerateArray().First().GetProperty("steps").EnumerateArray();

            foreach (var item in steps)
            {
                Direction direction = new Direction();
                direction.direction = item.GetProperty("instruction").ToString();
                direction.duration = item.GetProperty("duration").GetDouble();
                direction.distance = item.GetProperty("distance").GetDouble();

                var s_wp = item.GetProperty("way_points").EnumerateArray();

                List<Position> positions = new List<Position>();

                for (int i = s_wp.ElementAt(0).GetInt32(); i <= s_wp.ElementAt(1).GetInt32(); i++)
                {
                    Position p = new Position();

                    var coo = way_points.ElementAt(i).EnumerateArray();

                    p.latitude = coo.ElementAt(0).GetDouble();
                    p.longitude = coo.ElementAt(1).GetDouble();

                    positions.Add(p);
                }

                direction.way_points = positions;

                direction.start = positions.ElementAt(0);
                direction.end = positions.ElementAt(positions.Count - 1);

                directions.Add(direction);
            }

            trip.directions = directions;
            trip.duration = duration;
            trip.distance = distance;

            Console.WriteLine("Ready ! ");

        }
        else
        {
            Console.WriteLine("Error : " + response.StatusCode);
        }

        return trip;
    }



    static public List<VecteurStation> distanceStation(List<JCDStation> stations, Address ad)
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

    static public List<JCDStation> getStations(JCDContract jCDContract)
    {
        string query = "apiKey=" + "9e5337ebf66b3a6013ba8877317963b87f88f3de";
        string url = "https://api.jcdecaux.com/vls/v3/stations?contract=";
        url += jCDContract.name;
        string response = JCDecauxAPICall(url, query).Result;
        List<JCDStation> stationsContract = JsonSerializer.Deserialize<List<JCDStation>>(response);
        return stationsContract;

    }

    static public TripContrat getContrat(List<VecteurContrat> vc, int nbMax, Address ad)
    {
        if (vc.Count < nbMax)
        {
            nbMax = vc.Count;
        }

        List<TripContrat> tripContrats = new List<TripContrat>();

        for (int i = 0; i < nbMax; i++)
        {

            tripContrats.Add(new TripContrat() { trip = getTripToCityContract(vc.ElementAt(i).cityContract, ad), contract = vc.ElementAt(i).JCDContract });

        }

        tripContrats.Sort((TripContrat1, TripContrat2) => TripContrat1.trip.duration.CompareTo(TripContrat2.trip.duration));

        return tripContrats.ElementAt(0);
    }


    static public Trip getTripToCityContract(cityContract c, Address ad)
    {
        Console.WriteLine("Path calculation");

        CultureInfo enform = new CultureInfo("en-US");
        Trip trip = new Trip();

        string url = "https://api.openrouteservice.org/v2/directions/foot-walking?api_key=" + ORSkey + "&start=";
        url += ad.position.longitude + "," + ad.position.latitude;
        url += "&end=";
        url += c.position.longitude + "," + c.position.latitude;

        HttpClient client = new HttpClient();
        var response = client.GetAsync(url).Result;
        var result = response.Content.ReadAsStringAsync().Result;

        if (response.IsSuccessStatusCode)
        {
            var data = JsonDocument.Parse(result).RootElement.GetProperty("features").EnumerateArray().First();

            var way_points = data.GetProperty("geometry").GetProperty("coordinates").EnumerateArray();

            var duration = data.GetProperty("properties").GetProperty("summary").GetProperty("duration").GetDouble();
            var distance = data.GetProperty("properties").GetProperty("summary").GetProperty("distance").GetDouble();

            List<Direction> directions = new List<Direction>();

            var steps = data.GetProperty("properties").GetProperty("segments").EnumerateArray().First().GetProperty("steps").EnumerateArray();

            foreach (var item in steps)
            {
                Direction direction = new Direction();
                direction.direction = item.GetProperty("instruction").ToString();
                direction.duration = item.GetProperty("duration").GetDouble();
                direction.distance = item.GetProperty("distance").GetDouble();

                var s_wp = item.GetProperty("way_points").EnumerateArray();

                List<Position> positions = new List<Position>();

                for (int i = s_wp.ElementAt(0).GetInt32(); i <= s_wp.ElementAt(1).GetInt32(); i++)
                {
                    Position p = new Position();

                    var coo = way_points.ElementAt(i).EnumerateArray();

                    p.latitude = coo.ElementAt(0).GetDouble();
                    p.longitude = coo.ElementAt(1).GetDouble();

                    positions.Add(p);
                }

                direction.way_points = positions;

                direction.start = positions.ElementAt(0);
                direction.end = positions.ElementAt(positions.Count - 1);

                directions.Add(direction);
            }

            trip.directions = directions;
            trip.duration = duration;
            trip.distance = distance;

            Console.WriteLine("Ready ! ");

        }
        else
        {
            Console.WriteLine("Error : " + response.StatusCode);
        }

        return trip;


    }
}
*/