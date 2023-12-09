
package com.soap.ws.client.generated;

import javax.xml.bind.JAXBElement;
import javax.xml.bind.annotation.XmlAccessType;
import javax.xml.bind.annotation.XmlAccessorType;
import javax.xml.bind.annotation.XmlElementRef;
import javax.xml.bind.annotation.XmlType;


/**
 * <p>Java class for Direction complex type.
 * 
 * <p>The following schema fragment specifies the expected content contained within this class.
 * 
 * <pre>
 * &lt;complexType name="Direction"&gt;
 *   &lt;complexContent&gt;
 *     &lt;restriction base="{http://www.w3.org/2001/XMLSchema}anyType"&gt;
 *       &lt;sequence&gt;
 *         &lt;element name="direction" type="{http://www.w3.org/2001/XMLSchema}string" minOccurs="0"/&gt;
 *         &lt;element name="distance" type="{http://www.w3.org/2001/XMLSchema}double" minOccurs="0"/&gt;
 *         &lt;element name="duration" type="{http://www.w3.org/2001/XMLSchema}double" minOccurs="0"/&gt;
 *         &lt;element name="end" type="{http://schemas.datacontract.org/2004/07/soap_serv}Position" minOccurs="0"/&gt;
 *         &lt;element name="start" type="{http://schemas.datacontract.org/2004/07/soap_serv}Position" minOccurs="0"/&gt;
 *         &lt;element name="way_points" type="{http://schemas.datacontract.org/2004/07/soap_serv}ArrayOfPosition" minOccurs="0"/&gt;
 *       &lt;/sequence&gt;
 *     &lt;/restriction&gt;
 *   &lt;/complexContent&gt;
 * &lt;/complexType&gt;
 * </pre>
 * 
 * 
 */
@XmlAccessorType(XmlAccessType.FIELD)
@XmlType(name = "Direction", propOrder = {
    "direction",
    "distance",
    "duration",
    "end",
    "start",
    "wayPoints"
})
public class Direction {

    @XmlElementRef(name = "direction", namespace = "http://schemas.datacontract.org/2004/07/soap_serv", type = JAXBElement.class, required = false)
    protected JAXBElement<String> direction;
    protected Double distance;
    protected Double duration;
    @XmlElementRef(name = "end", namespace = "http://schemas.datacontract.org/2004/07/soap_serv", type = JAXBElement.class, required = false)
    protected JAXBElement<Position> end;
    @XmlElementRef(name = "start", namespace = "http://schemas.datacontract.org/2004/07/soap_serv", type = JAXBElement.class, required = false)
    protected JAXBElement<Position> start;
    @XmlElementRef(name = "way_points", namespace = "http://schemas.datacontract.org/2004/07/soap_serv", type = JAXBElement.class, required = false)
    protected JAXBElement<ArrayOfPosition> wayPoints;

    /**
     * Gets the value of the direction property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link String }{@code >}
     *     
     */
    public JAXBElement<String> getDirection() {
        return direction;
    }

    /**
     * Sets the value of the direction property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link String }{@code >}
     *     
     */
    public void setDirection(JAXBElement<String> value) {
        this.direction = value;
    }

    /**
     * Gets the value of the distance property.
     * 
     * @return
     *     possible object is
     *     {@link Double }
     *     
     */
    public Double getDistance() {
        return distance;
    }

    /**
     * Sets the value of the distance property.
     * 
     * @param value
     *     allowed object is
     *     {@link Double }
     *     
     */
    public void setDistance(Double value) {
        this.distance = value;
    }

    /**
     * Gets the value of the duration property.
     * 
     * @return
     *     possible object is
     *     {@link Double }
     *     
     */
    public Double getDuration() {
        return duration;
    }

    /**
     * Sets the value of the duration property.
     * 
     * @param value
     *     allowed object is
     *     {@link Double }
     *     
     */
    public void setDuration(Double value) {
        this.duration = value;
    }

    /**
     * Gets the value of the end property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link Position }{@code >}
     *     
     */
    public JAXBElement<Position> getEnd() {
        return end;
    }

    /**
     * Sets the value of the end property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link Position }{@code >}
     *     
     */
    public void setEnd(JAXBElement<Position> value) {
        this.end = value;
    }

    /**
     * Gets the value of the start property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link Position }{@code >}
     *     
     */
    public JAXBElement<Position> getStart() {
        return start;
    }

    /**
     * Sets the value of the start property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link Position }{@code >}
     *     
     */
    public void setStart(JAXBElement<Position> value) {
        this.start = value;
    }

    /**
     * Gets the value of the wayPoints property.
     * 
     * @return
     *     possible object is
     *     {@link JAXBElement }{@code <}{@link ArrayOfPosition }{@code >}
     *     
     */
    public JAXBElement<ArrayOfPosition> getWayPoints() {
        return wayPoints;
    }

    /**
     * Sets the value of the wayPoints property.
     * 
     * @param value
     *     allowed object is
     *     {@link JAXBElement }{@code <}{@link ArrayOfPosition }{@code >}
     *     
     */
    public void setWayPoints(JAXBElement<ArrayOfPosition> value) {
        this.wayPoints = value;
    }

}
