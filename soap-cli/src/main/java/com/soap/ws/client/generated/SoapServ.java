
package com.soap.ws.client.generated;

import java.net.MalformedURLException;
import java.net.URL;
import javax.xml.namespace.QName;
import javax.xml.ws.Service;
import javax.xml.ws.WebEndpoint;
import javax.xml.ws.WebServiceClient;
import javax.xml.ws.WebServiceException;
import javax.xml.ws.WebServiceFeature;


/**
 * This class was generated by the JAX-WS RI.
 * JAX-WS RI 2.3.2
 * Generated source version: 2.2
 * 
 */
@WebServiceClient(name = "SoapServ", targetNamespace = "http://tempuri.org/", wsdlLocation = "http://localhost:8090/SoapServ/letsgobiking?wsdl")
public class SoapServ
    extends Service
{

    private final static URL SOAPSERV_WSDL_LOCATION;
    private final static WebServiceException SOAPSERV_EXCEPTION;
    private final static QName SOAPSERV_QNAME = new QName("http://tempuri.org/", "SoapServ");

    static {
        URL url = null;
        WebServiceException e = null;
        try {
            url = new URL("http://localhost:8090/SoapServ/letsgobiking?wsdl");
        } catch (MalformedURLException ex) {
            e = new WebServiceException(ex);
        }
        SOAPSERV_WSDL_LOCATION = url;
        SOAPSERV_EXCEPTION = e;
    }

    public SoapServ() {
        super(__getWsdlLocation(), SOAPSERV_QNAME);
    }

    public SoapServ(WebServiceFeature... features) {
        super(__getWsdlLocation(), SOAPSERV_QNAME, features);
    }

    public SoapServ(URL wsdlLocation) {
        super(wsdlLocation, SOAPSERV_QNAME);
    }

    public SoapServ(URL wsdlLocation, WebServiceFeature... features) {
        super(wsdlLocation, SOAPSERV_QNAME, features);
    }

    public SoapServ(URL wsdlLocation, QName serviceName) {
        super(wsdlLocation, serviceName);
    }

    public SoapServ(URL wsdlLocation, QName serviceName, WebServiceFeature... features) {
        super(wsdlLocation, serviceName, features);
    }

    /**
     * 
     * @return
     *     returns ISoapServ
     */
    @WebEndpoint(name = "BasicHttpBinding_ISoapServ")
    public ISoapServ getBasicHttpBindingISoapServ() {
        return super.getPort(new QName("http://tempuri.org/", "BasicHttpBinding_ISoapServ"), ISoapServ.class);
    }

    /**
     * 
     * @param features
     *     A list of {@link javax.xml.ws.WebServiceFeature} to configure on the proxy.  Supported features not in the <code>features</code> parameter will have their default values.
     * @return
     *     returns ISoapServ
     */
    @WebEndpoint(name = "BasicHttpBinding_ISoapServ")
    public ISoapServ getBasicHttpBindingISoapServ(WebServiceFeature... features) {
        return super.getPort(new QName("http://tempuri.org/", "BasicHttpBinding_ISoapServ"), ISoapServ.class, features);
    }

    private static URL __getWsdlLocation() {
        if (SOAPSERV_EXCEPTION!= null) {
            throw SOAPSERV_EXCEPTION;
        }
        return SOAPSERV_WSDL_LOCATION;
    }

}