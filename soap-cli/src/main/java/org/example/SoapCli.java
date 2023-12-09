package org.example;
import com.soap.ws.client.generated.*;
import org.apache.activemq.ActiveMQConnectionFactory;

import javax.jms.*;
import java.util.List;
import java.util.Scanner;

public class SoapCli {

    static Address start;
    static Address end;
    static ContractAMQTrip contract;

    static String queueName = "";
    static Scanner scanner = new Scanner(System.in);
    static SoapServ service = new SoapServ();
    static ISoapServ port = service.getBasicHttpBindingISoapServ();


    public static void main(String[] args) {
        boolean exit = false;

        printBienvenue();

        while(!exit) {
            printMenu();
            String s = scanner.nextLine();
            if(s.equals("q")) {
                exit = true;
            }
            else if(s.equals("1")) {
                renseignerAdresse();
            }
            else if(s.equals("a")) {
                hack("6 Quai de la Tourelle, 95000 Cergy", "36 Rue de Pontoise, 95000 Cergy");
            }
            else if(s.equals("z")) {
                hack("260 chemin des vergers Roquebrune sur argens Var France", "Paris tour eiffel");
            }
            else {
                System.out.println("Choix invalide");
            }
        }
    }

    public static void hack(String a1, String a2) {
        ArrayOfAddress arrayOfAddress = port.getAddresses(a1);
        start = arrayOfAddress.getAddress().get(0);
        System.out.println("start : " + start.getFormatted().getValue());
        arrayOfAddress = port.getAddresses(a2);
        end = arrayOfAddress.getAddress().get(0);
        System.out.println("end : " + end.getFormatted().getValue());
        getPath();
        printDivideBar();
        System.out.println("Votre itinéraire :");
        printDivideBar();
        Thread t = new Thread(SoapCli::printTrip);
        t.start();
        printDivideBar();
        System.out.println("Votre intinéraire sur la carte :");
        showMap();
    }

    public static void printBienvenue() {
        System.out.println("Bienvenue sur LETS GO BIKING !");
    }

    public static void printMenu() {
        System.out.println("1. Trouver un itinéraire");
        System.out.println("q pour quitter");
    }

    public static void printActionChoixAdresse() {
        System.out.println("1. Renseigner une adresse de départ");
        System.out.println("2. Renseigner une adresse d'arrivée");
        System.out.println("v pour valider");
        System.out.println("q pour quitter");
    }
    public static void printChoixAdresseDepart() {
        System.out.println("Renseignez votre adresse de départ :");
    }

    public static void printChoixAdresseArrivee() {
        System.out.println("Renseignez votre adresse d'arrivée :");
    }

    public static void printChoixAdresseDepartArrivee() {
        System.out.println("Les adresses renseignées sont :");
        System.out.println("Adresse de départ :" + start.getFormatted().getValue());
        System.out.println("Adresse d'arrivée :" + end.getFormatted().getValue());
    }

    public static void renseignerAdresseDepart() {
        printChoixAdresseDepart();
        Scanner sca = new Scanner(System.in);
        String s = sca.nextLine();
        if(s.equals("q")) {
            printMenu();
        }
        else {
            start = selectAdresse(s);
        }
    }

    public static void renseignerAdresseArrivee() {
        printChoixAdresseArrivee();
        Scanner sca = new Scanner(System.in);
        String s = sca.nextLine();
        if(s.equals("q")) {
            printMenu();
        }
        else {
            end = selectAdresse(s);
        }
    }

    public static Address selectAdresse(String s)
    {
        Scanner sca = new Scanner(System.in);
        Address a = null;
        try {
            ArrayOfAddress arrayOfAddress = port.getAddresses(s);
            List<Address> addresses = arrayOfAddress.getAddress();
            int i = 1;

            if(addresses.size() == 0) {
                System.out.println("Aucune adresse trouvée");
                System.out.println("Saisissez une adresse plus détaillée");
                return selectAdresse(sca.nextLine());
            }

            System.out.println("Choisissez votre adresse : ");
            for (Address address : addresses) {
                System.out.println( i + ". " + address.getFormatted().getValue());
                i++;
            }
            System.out.println("r pour ressaisir l'adresse");
            System.out.println("q pour quitter");
            String choice = sca.nextLine();

            if(choice.equals("q")) {
                printMenu();
            }
            else if(choice.equals("r")) {
                System.out.println("Saisissez de nouveau une adresse :");

                return selectAdresse(sca.nextLine());
            }
            else {
                a = addresses.get(Integer.parseInt(choice) - 1);
            }

        } catch (Exception e) {
            e.printStackTrace();
        }
        return a;
    }

    public static void renseignerAdresse() {
        Scanner sca = new Scanner(System.in);
        if(start == null) {
            renseignerAdresseDepart();
            System.out.println("start : " + start.getFormatted().getValue());
        }
        if(end == null) {
            renseignerAdresseArrivee();
        }
        if(start != null && end != null) {
            printChoixAdresseDepartArrivee();
            printDivideBar();
            printActionChoixAdresse();
            String s = sca.nextLine();
            switch (s) {
                case "1":
                    renseignerAdresseDepart();
                    break;
                case "2":
                    renseignerAdresseArrivee();
                    break;
                case "v":
                    getPath();
                    printDivideBar();
                    System.out.println("Votre itinéraire :");
                    printDivideBar();
                    Thread t = new Thread(SoapCli::printTrip);
                    t.start();
                    printDivideBar();
                    System.out.println("Votre intinéraire sur la carte :");
                    showMap();
                    printDivideBar();
                    break;
                case "q":
                    return;
                default:
                    System.out.println("Choix invalide");
                    break;
            }
        }
    }

    private static void printDivideBar() {
        System.out.println("--------------------------------------------------");
    }

    public static void getPath() {
        try {
            contract = port.getPath(start, end);
            queueName = contract.getNameQueue().getValue();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public static void printTrip() {
        if(contract != null) {
            if(contract.isIsQueue())
            {
                try {
                    // Créez une connexion à ActiveMQ
                    ActiveMQConnectionFactory connectionFactory = new ActiveMQConnectionFactory("tcp://localhost:6465");
                    Connection connection = connectionFactory.createConnection();
                    connection.start();

                    // Créez une session
                    Session session = connection.createSession(false, Session.AUTO_ACKNOWLEDGE);

                    // Utilisez l'objet IDestination pour créer une destination ActiveMQ
                    Destination destination = session.createQueue(queueName);

                    // Créez un consommateur de messages pour cette destination
                    MessageConsumer consumer = session.createConsumer(destination);

                    // Attendez la réception d'un message

                    Message message = consumer.receive();


                    // Traitez le message (exemple : affichez le contenu du message)
                    while(message instanceof TextMessage) {
                        System.out.println("Received message: " + ((TextMessage) message).getText());
                        message = consumer.receive();
                    }
                    // close
                    consumer.close();
                    session.close();
                    connection.close();

                } catch (JMSException e) {
                    throw new RuntimeException(e);
                }
            }
            else {
                for(Trip t : contract.getAllTrip().getValue().getTrip())
                {
                    for(Direction d : t.getDirections().getValue().getDirection())
                    {
                        System.out.println(d.getDirection().getValue());
                    }
                }
            }

        }
        else
        {
            System.out.println("Aucun itinéraire trouvé");
        }
    }
    public static void showMap(){
        CreateMap createMap = new CreateMap(contract.getAllTrip().getValue().getTrip());
    }

}
