using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// add assembly System.ServiceModel  and using for the corresponding model
using System.ServiceModel;
using System.Net.Http;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Apache.NMS;
using System.Runtime.Serialization;


namespace soap_serv
{

    /** Interface du serveur SOAP
     ** 
     ** @param GetAddresses : retourne une liste d'adresse correspondant à l'adresse en paramètre
     ** @param getPath : retourne un objet Contract_AMQ_Trip contenant une liste de trajet entre deux adresses
     ** 
     **/
    [ServiceContract()]
    interface ISoapServ
    {
        [OperationContract()]
        List<Address> GetAddresses(string adress);


        [OperationContract()]
        Contract_AMQ_Trip getPath(Address start, Address end);


    }


    /** Objet retourné par le serveur vers les clients
     * @param nameQueue : nom de la queue
     * @param isQueue : true si la queue existe, false sinon
     * @param allTrip : liste de trajet
     * 
     */
    [DataContract]
    public class Contract_AMQ_Trip
    {
        [DataMember]
        public string nameQueue { get; set; }
        [DataMember]
        public bool isQueue { get; set; }
        [DataMember]
        public List<Trip> allTrip { get; set; }
        public Contract_AMQ_Trip()
        {
            allTrip = new List<Trip>();
        }

    }

    /** Position geo
     * 
     */
    [DataContract]
    public class Position
    {
        [DataMember]
        public Double latitude { get; set; }
        [DataMember]
        public Double longitude { get; set; }

        public override string ToString()
        {
            return "lat : " + latitude + "; long : " + longitude + "\n";
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) return false;
            if (!(obj is Position)) return false;
            Position position = (Position)obj;
            return position.latitude.Equals(latitude) && position.longitude.Equals(longitude);
        }
    }

    /** Adresse
     * @param position : position geo
     * @param formatted : adresse formatté
     * @param city : ville
     * @param country : pays
     * @param road : rue
     * @param houseNumber : numero de rue
     */
    [DataContract]
    public class Address
    {
        [DataMember]
        public Position position { get; set; }
        [DataMember]
        public string formatted { get; set; }
        [DataMember]
        public string city { get; set; }
        [DataMember]
        public string country { get; set; }
        [DataMember]
        public string road { get; set; }
        [DataMember]
        public string houseNumber { get; set; }

        public override String ToString()
        {
            string s = "";
            if (!houseNumber.Equals("-1"))
            {
                s += houseNumber + " ";
            }
            s += road + ", " + formatted;
            return s;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null) return false;
            if (!(obj is Address)) return false;
            Address address = (Address)obj;
            return address.formatted.Equals(formatted) && address.road.Equals(road) && address.houseNumber.Equals(houseNumber) && address.country.Equals(country);
        }

        public override int GetHashCode()
        {
            return formatted.GetHashCode() + road.GetHashCode() + houseNumber.GetHashCode() + country.GetHashCode();
        }
    }


    /** Direction
     * est composé d'une liste de position (way_points) et de deux positions (start et end)
     * correspond à un trajet entre deux points
     * 
     * @param direction : direction à prendre
     * @param start : position de départ
     * @param way_points : liste de position intermédiaire
     * @param end : position d'arrivée
     * @param duration : durée du trajet en seconde
     * @param distance : distance du trajet en mètre
     * 
     */
    [DataContract]
    public class Direction
    {
        [DataMember]
        public string direction { get; set; }
        [DataMember]
        public Position start { get; set; }
        [DataMember]
        public List<Position> way_points { get; set; }
        [DataMember]
        public Position end { get; set; }
        [DataMember]
        public Double duration { get; set; }
        [DataMember]
        public Double distance { get; set; }


        public override string ToString()
        {
            string s = "";
            s += direction + "\n";
            s += "start : " + start + "\n";
            s += "end : " + end + "\n";
            s += "\n";
            return s;
        }
    }

    /** Trajet
     * @param directions : liste de direction
     * @param type : type de trajet (foot_walking ou cycling_regular)
     * @param duration : durée du trajet en seconde
     * @param distance : distance du trajet en mètre
     * 
     */
    [DataContract]
    public class Trip
    {
        [DataMember]
        public List<Direction> directions { get; set; }
        [DataMember]
        public TypeOfTrip type { get; set; }
        [DataMember]
        public Double duration { get; set; }
        [DataMember]
        public Double distance { get; set; }

        public Trip(TypeOfTrip type)
        {
            this.type = type;
        }


        public string toString()
        {
            string s = "duration : " + duration + "s;\n distance : " + distance + "m; \n";
            if (directions == null)
            {
                return s;
            }
            foreach (Direction direction in directions)
            {
                s += direction.direction + "\n";
            }
            return s;
        }

        public override string ToString()
        {
            string s = "duration : " + duration + "s;\n distance : " + distance + "m; \n";
            if (directions == null)
            {
                return s;
            }
            foreach (Direction direction in directions)
            {
                s += direction.direction + "\n";
            }
            return s;
        }
    }


    /**  DO NOT RENAME ENUMMEMBER 
     * 
     * Type of trip used to calculate different type of trip
     * used in the url of the request & modified by replacing _ by -
     * 
     */
    [DataContract]
    public enum TypeOfTrip
    {
        [EnumMember(Value = "foot_walking")]
        foot_walking,
        [EnumMember(Value = "cycling_regular")]
        cycling_regular,
    }
}
