
package com.soap.ws.client.generated;

import javax.xml.bind.JAXBElement;
import javax.xml.bind.annotation.XmlAccessType;
import javax.xml.bind.annotation.XmlAccessorType;
import javax.xml.bind.annotation.XmlElementRef;
import javax.xml.bind.annotation.XmlType;


/**
 * <p>Java class for Contract_AMQ_Trip complex type.
 * 
 * <p>The following schema fragment specifies the expected content contained within this class.
 * 
 * <pre>
 * &lt;complexType name="Contract_AMQ_Trip"&gt;
 *   &lt;complexContent&gt;
 *     &lt;restriction base="{http://www.w3.org/2001/XMLSchema}anyType"&gt;
 *       &lt;sequence&gt;
 *         &lt;element name="allTrip" type="{http://schemas.datacontract.org/2004/07/soap_serv}ArrayOfTrip" minOccurs="0"/&gt;
 *         &lt;element name="isQueue" type="{http://www.w3.org/2001/XMLSchema}boolean" minOccurs="0"/&gt;
 *         &lt;element name="nameQueue" type="{http://www.w3.org/2001/XMLSchema}string" minOccurs="0"/&gt;
 *       &lt;/sequence&gt;
 *     &lt;/restriction&gt;
 *   &lt;/complexContent&gt;
 * &lt;/complexType&gt;
 * </pre>
 * 
 * 
 */
@XmlAccessorType(XmlAccessType.FIELD)
@XmlType(name = "Contract_AMQ_Trip", propOrder = {
    "allTrip",
    "isQueue",
    "nameQueue"
})
public class ContractAMQTrip {

    @XmlElementRef(name = "allTrip", namespace = "http://schemas.datacontract.org/2004/07/soap_serv", type = JAXBElement.class, required = false)
    protected JAXBElement<ArrayOfTrip> allTrip;
    protected Boolean isQueue;
    @XmlElementRef(name = "nameQueue", namespace = "http://schemas.datacontract.org/2004/07/soap_serv", type = JAXBElement.class, required = false)
    protected JAXBElement<String> nameQueue;

    /**
     * Gets the value of the allTrip property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link ArrayOfTrip }{@code >}
     *     
     */
    public JAXBElement<ArrayOfTrip> getAllTrip() {
        return allTrip;
    }

    /**
     * Sets the value of the allTrip property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link ArrayOfTrip }{@code >}
     *     
     */
    public void setAllTrip(JAXBElement<ArrayOfTrip> value) {
        this.allTrip = value;
    }

    /**
     * Gets the value of the isQueue property.
     * 
     * @return
     *     possible object is
     *     {@link Boolean }
     *     
     */
    public Boolean isIsQueue() {
        return isQueue;
    }

    /**
     * Sets the value of the isQueue property.
     * 
     * @param value
     *     allowed object is
     *     {@link Boolean }
     *     
     */
    public void setIsQueue(Boolean value) {
        this.isQueue = value;
    }

    /**
     * Gets the value of the nameQueue property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link String }{@code >}
     *     
     */
    public JAXBElement<String> getNameQueue() {
        return nameQueue;
    }

    /**
     * Sets the value of the nameQueue property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link String }{@code >}
     *     
     */
    public void setNameQueue(JAXBElement<String> value) {
        this.nameQueue = value;
    }

}
