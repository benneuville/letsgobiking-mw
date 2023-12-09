using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Runtime.Serialization;
using System.ServiceModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Numerics;

namespace soap_serv
{


    /** relation Contrat - trajet
     **/
    public class TripContrat
    {
        public Trip trip { get; set; }
        public JCDContract contract { get; set; }
    }

    /** relation trajet - station
     **/
    public class TripStation
    {
        public Trip trip { get; set; }
        public JCDStation station { get; set; }
    }

    //Vecteurs

    /** Vecteur vers un contrat
     **/
    class VecteurContrat
    {
        public JCDContract JCDContract { get; set; }
        public Vecteur vecteur { get; set; }
        public cityContract cityContract { get; set; }
        public VecteurContrat(JCDContract jCDContract, Vecteur vecteur, cityContract cityContract)
        {
            JCDContract = jCDContract;
            this.vecteur = vecteur;
            this.cityContract = cityContract;
        }

    }

    /** Vecteur vers une station
     **/
    class VecteurStation
    {
        public JCDStation JCDStation { get; set; }
        public Vecteur vecteur { get; set; }
        public VecteurStation(JCDStation jCDStation, Vecteur vecteur)
        {
            JCDStation = jCDStation;
            this.vecteur = vecteur;
        }
    }

    /** Vecteur Base
     * @param X : X
     * @param Y : Y
     **/
    public class Vecteur : IComparable<Vecteur>
    {

        public double X { get; set; }
        public double Y { get; set; }

        public Vecteur(Position depart, Position arrivee)
        {
            this.X = arrivee.longitude - depart.longitude;
            this.Y = arrivee.latitude - depart.latitude;
        }

        public double GetTaille()
        {
            return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
        }

        public int CompareTo(Vecteur other)
        {
            return other.GetTaille().CompareTo(this.GetTaille());
        }
    }

    /** Ville : relation nom - position
     **/
    public class cityContract
    {
        public string name { get; set; }
        public Position position { get; set; }
    }

    //JCDecaux
    public class JCDContract
    {
        public string name { get; set; }

        public List<cityContract> cities { get; set; } = new List<cityContract>();

    }

    public class JCDStation
    {
        public int number { get; set; }
        public string name { get; set; }
        public Position position { get; set; }

        public int stands { get; set; }

        public int bikes { get; set; }

        public int capacity { get; set; }
    }
}
