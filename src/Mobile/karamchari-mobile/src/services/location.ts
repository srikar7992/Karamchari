import * as Location from "expo-location";

export const getLocation = async () => {
  const { status } = await Location.requestForegroundPermissionsAsync();

  if (status !== "granted") {
    throw new Error("Location permission denied");
  }

  const loc = await Location.getCurrentPositionAsync({
    accuracy: Location.Accuracy.High,
  });

  return {
    lat: loc.coords.latitude,
    lon: loc.coords.longitude,
    accuracy: loc.coords.accuracy,
  };
};
