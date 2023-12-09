
package com.soap.ws.client.generated;

import javax.xml.bind.annotation.XmlEnum;
import javax.xml.bind.annotation.XmlEnumValue;
import javax.xml.bind.annotation.XmlType;


/**
 * <p>Java class for TypeOfTrip.
 * 
 * <p>The following schema fragment specifies the expected content contained within this class.
 * <p>
 * <pre>
 * &lt;simpleType name="TypeOfTrip"&gt;
 *   &lt;restriction base="{http://www.w3.org/2001/XMLSchema}string"&gt;
 *     &lt;enumeration value="foot_walking"/&gt;
 *     &lt;enumeration value="cycling_regular"/&gt;
 *   &lt;/restriction&gt;
 * &lt;/simpleType&gt;
 * </pre>
 * 
 */
@XmlType(name = "TypeOfTrip")
@XmlEnum
public enum TypeOfTrip {

    @XmlEnumValue("foot_walking")
    FOOT_WALKING("foot_walking"),
    @XmlEnumValue("cycling_regular")
    CYCLING_REGULAR("cycling_regular");
    private final String value;

    TypeOfTrip(String v) {
        value = v;
    }

    public String value() {
        return value;
    }

    public static TypeOfTrip fromValue(String v) {
        for (TypeOfTrip c: TypeOfTrip.values()) {
            if (c.value.equals(v)) {
                return c;
            }
        }
        throw new IllegalArgumentException(v);
    }

}
