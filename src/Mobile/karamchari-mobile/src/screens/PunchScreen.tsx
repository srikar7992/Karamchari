import React, { useState, useEffect, useMemo } from "react";
import { View, Text, Button, Alert, StyleSheet, ActivityIndicator, Dimensions } from "react-native";
import MapView, { Marker, Circle } from "react-native-maps";
import { getLocation } from "../services/location";
import { usePunchStore } from "../store/punchStore";
import { v4 as uuidv4 } from "uuid";

// Minimal Geofence Helper
const haversine = (lat1: number, lon1: number, lat2: number, lon2: number) => {
  const R = 6371000;
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLon = (lon2 - lon1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) ** 2 + 
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * 
            Math.sin(dLon / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
};

// Placeholder Boundary (Hyderabad HQ)
const BOUNDARY = {
  lat: 17.385,
  lon: 78.486,
  radius: 200, // 200 meters
};

export default function PunchScreen() {
  const [localLoading, setLocalLoading] = useState(false);
  const [currentLoc, setCurrentLoc] = useState<{lat: number, lon: number, accuracy: number} | null>(null);
  const { addPunch, isSyncing, pendingCount, refreshPendingCount } = usePunchStore();

  useEffect(() => {
    refreshPendingCount();
    updateLocation();
    
    const interval = setInterval(updateLocation, 30000); // Update every 30s
    return () => clearInterval(interval);
  }, []);

  const updateLocation = async () => {
    try {
      const loc = await getLocation();
      setCurrentLoc({
        ...loc,
        accuracy: loc.accuracy ?? 0
      });
    } catch (e) {}
  };

  const distance = useMemo(() => {
    if (!currentLoc) return Infinity;
    return haversine(currentLoc.lat, currentLoc.lon, BOUNDARY.lat, BOUNDARY.lon);
  }, [currentLoc]);

  const isInside = distance <= BOUNDARY.radius;

  const handlePunch = async () => {
    try {
      setLocalLoading(true);
      const loc = await getLocation();

      await addPunch({
        id: uuidv4(),
        employeeId: "00000000-0000-0000-0000-000000000001",
        lat: loc.lat,
        lon: loc.lon,
        accuracy: loc.accuracy ?? 0,
        eventTimestamp: new Date().toISOString(),
      });

      Alert.alert("Success", "Punch recorded locally and queued for sync.");
    } catch (err: any) {
      Alert.alert("Error", err.message || "Failed to capture location");
    } finally {
      setLocalLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      {pendingCount > 0 && (
        <View style={styles.syncBanner}>
          <Text style={styles.syncText}>
            {isSyncing ? "🔄 Syncing data..." : `⏳ ${pendingCount} punch(es) pending sync`}
          </Text>
        </View>
      )}

      {currentLoc && (
        <MapView
          style={styles.map}
          initialRegion={{
            latitude: currentLoc.lat,
            longitude: currentLoc.lon,
            latitudeDelta: 0.005,
            longitudeDelta: 0.005,
          }}
        >
          <Marker coordinate={{ latitude: currentLoc.lat, longitude: currentLoc.lon }} title="You" pinColor="blue" />
          <Circle
            center={{ latitude: BOUNDARY.lat, longitude: BOUNDARY.lon }}
            radius={BOUNDARY.radius}
            strokeColor={isInside ? "rgba(0, 255, 0, 0.5)" : "rgba(255, 0, 0, 0.5)"}
            fillColor={isInside ? "rgba(0, 255, 0, 0.1)" : "rgba(255, 0, 0, 0.1)"}
          />
        </MapView>
      )}

      <View style={styles.controlPanel}>
        <Text style={styles.statusText}>
          {isInside ? "✅ Inside Work Zone" : "❌ Outside Work Zone"}
        </Text>
        <Text style={styles.distText}>
          Distance: {Math.round(distance)}m (Limit: {BOUNDARY.radius}m)
        </Text>
        
        {localLoading ? (
          <ActivityIndicator size="large" color="#007AFF" />
        ) : (
          <View style={styles.buttonContainer}>
            <Button
              title="Punch In"
              onPress={handlePunch}
              color={isInside ? "#34C759" : "#8E8E93"}
              disabled={!isInside}
            />
          </View>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F2F2F7",
  },
  map: {
    width: "100%",
    height: Dimensions.get("window").height * 0.65,
  },
  syncBanner: {
    position: "absolute",
    top: 50,
    zIndex: 10,
    backgroundColor: "rgba(255, 149, 0, 0.9)",
    padding: 10,
    borderRadius: 8,
    alignSelf: "center",
  },
  syncText: {
    color: "white",
    fontWeight: "bold",
  },
  controlPanel: {
    flex: 1,
    padding: 20,
    backgroundColor: "white",
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: -2 },
    shadowOpacity: 0.1,
    shadowRadius: 10,
    elevation: 5,
    alignItems: "center",
  },
  statusText: {
    fontSize: 20,
    fontWeight: "bold",
    marginBottom: 5,
  },
  distText: {
    fontSize: 14,
    color: "#8E8E93",
    marginBottom: 20,
  },
  buttonContainer: {
    width: "100%",
    borderRadius: 12,
    overflow: "hidden",
  }
});
