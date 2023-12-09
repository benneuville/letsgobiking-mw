package org.example;

import org.jxmapviewer.viewer.DefaultWaypoint;
import org.jxmapviewer.viewer.GeoPosition;

import java.awt.*;

/**
 * A waypoint that also has a color and a label
 * @author Martin Steiger
 */
public class ColoredWP extends DefaultWaypoint {
    private final String label;
    private final Color color;

    /**
     * @param coord the coordinate
     * @param label the text
     * @param color the color
     */
    public ColoredWP(GeoPosition coord, String label, Color color) {
        super(coord);
        this.label = label;
        this.color = color;
    }

    /**
     * @return the label text
     */
    public String getLabel() {
        return label;
    }

    /**
     * @return the color
     */
    public Color getColor() {
        return color;
    }
}
