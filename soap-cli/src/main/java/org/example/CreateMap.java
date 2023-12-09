package org.example;

import com.soap.ws.client.generated.Direction;
import com.soap.ws.client.generated.Position;
import com.soap.ws.client.generated.Trip;
import com.soap.ws.client.generated.TypeOfTrip;
import org.jxmapviewer.JXMapViewer;
import org.jxmapviewer.OSMTileFactoryInfo;
import org.jxmapviewer.painter.CompoundPainter;
import org.jxmapviewer.painter.Painter;
import org.jxmapviewer.viewer.*;

import javax.swing.*;
import java.awt.*;
import java.util.List;
import java.util.*;

/**
 * A simple sample application that shows
 * a OSM map of Europe containing a route with waypoints
 * @author Martin Steiger
 */
public class CreateMap
{

    private static final HashMap<String, Color> C = new HashMap<String, Color>() {{
        put(TypeOfTrip.FOOT_WALKING.value(), Color.RED);
        put(TypeOfTrip.CYCLING_REGULAR.value(), Color.BLUE);
    }};

    public CreateMap(List<Trip> trip){
        createPoint(trip);
    }


    public void createPoint(List<Trip> trip){
        JXMapViewer mapViewer = new JXMapViewer();

        // Display the viewer in a JFrame
        JFrame frame = new JFrame("OK BEN GELDİM");
        frame.getContentPane().add(mapViewer);
        frame.setSize(800, 600);
        frame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        frame.setVisible(true);

        // Create a TileFactoryInfo for OpenStreetMap
        TileFactoryInfo info = new OSMTileFactoryInfo();
        DefaultTileFactory tileFactory = new DefaultTileFactory(info);
        tileFactory.setThreadPoolSize(8);
        mapViewer.setTileFactory(tileFactory);

        List<GeoPosition> allpoint = new ArrayList<GeoPosition>();
        List<RoutePainter> routePainters = new ArrayList<RoutePainter>();

        Set<ColoredWP> waypoints = new HashSet<ColoredWP>();
        for (Trip t : trip) {
            List<GeoPosition> track = new ArrayList<GeoPosition>();
            for (Direction d : t.getDirections().getValue().getDirection()) {
                for (Position p : d.getWayPoints().getValue().getPosition()) {
                    track.add(new GeoPosition(p.getLatitude(), p.getLongitude()));
                }
            }
            allpoint.addAll(track);
           routePainters.add(new RoutePainter(track, C.get(t.getType().value())));
        }



        // Set the focus
        mapViewer.zoomToBestFit(new HashSet<GeoPosition>(allpoint), 0.7);

        SelectionAdapter sa = new SelectionAdapter(mapViewer);

        mapViewer.addMouseListener(sa);
        mapViewer.addMouseMotionListener(sa);
        // Create waypoints from the geo-positions

        waypoints.add(new ColoredWP(allpoint.get(0), "Start", Color.GREEN));
        waypoints.add(new ColoredWP(allpoint.get(allpoint.size() - 1), "End", Color.RED));

        // Create a waypoint painter that takes all the waypoints
        WaypointPainter<ColoredWP> waypointPainter = new WaypointPainter<ColoredWP>();

        // Create a compound painter that uses both the route-painter and the waypoint-painter
        List<Painter<JXMapViewer>> painters = new ArrayList<Painter<JXMapViewer>>(routePainters);
        painters.add(waypointPainter);

        waypointPainter.setWaypoints(waypoints);
        waypointPainter.setRenderer(new RenderWP());


        CompoundPainter<JXMapViewer> painter = new CompoundPainter<JXMapViewer>(painters);
        mapViewer.setOverlayPainter(painter);
    }
}
