import "react-native-get-random-values";
import { useEffect } from "react";
import { SafeAreaView, StyleSheet } from "react-native";
import { StatusBar } from "expo-status-bar";
import PunchScreen from "./src/screens/PunchScreen";
import { getDb } from "./src/storage/db";
import { registerBackgroundSync } from "./src/services/syncService";

export default function App() {
  useEffect(() => {
    // Initialize Offline Foundation
    const init = async () => {
      await getDb();
      await registerBackgroundSync();
    };
    init();
  }, []);

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="auto" />
      <PunchScreen />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F2F2F7",
  },
});

