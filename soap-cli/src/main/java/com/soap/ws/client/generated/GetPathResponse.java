
package com.soap.ws.client.generated;

import javax.xml.bind.JAXBElement;
import javax.xml.bind.annotation.XmlAccessType;
import javax.xml.bind.annotation.XmlAccessorType;
import javax.xml.bind.annotation.XmlElementRef;
import javax.xml.bind.annotation.XmlRootElement;
import javax.xml.bind.annotation.XmlType;


/**
 * <p>Java class for anonymous complex type.
 * 
 * <p>The following schema fragment specifies the expected content contained within this class.
 * 
 * <pre>
 * &lt;complexType&gt;
 *   &lt;complexContent&gt;
 *     &lt;restriction base="{http://www.w3.org/2001/XMLSchema}anyType"&gt;
 *       &lt;sequence&gt;
 *         &lt;element name="getPathResult" type="{http://schemas.datacontract.org/2004/07/soap_serv}Contract_AMQ_Trip" minOccurs="0"/&gt;
 *       &lt;/sequence&gt;
 *     &lt;/restriction&gt;
 *   &lt;/complexContent&gt;
 * &lt;/complexType&gt;
 * </pre>
 * 
 * 
 */
@XmlAccessorType(XmlAccessType.FIELD)
@XmlType(name = "", propOrder = {
    "getPathResult"
})
@XmlRootElement(name = "getPathResponse", namespace = "http://tempuri.org/")
public class GetPathResponse {

    @XmlElementRef(name = "getPathResult", namespace = "http://tempuri.org/", type = JAXBElement.class, required = false)
    protected JAXBElement<ContractAMQTrip> getPathResult;

    /**
     * Gets the value of the getPathResult property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link ContractAMQTrip }{@code >}
     *     
     */
    public JAXBElement<ContractAMQTrip> getGetPathResult() {
        return getPathResult;
    }

    /**
     * Sets the value of the getPathResult property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link ContractAMQTrip }{@code >}
     *     
     */
    public void setGetPathResult(JAXBElement<ContractAMQTrip> value) {
        this.getPathResult = value;
    }

}
