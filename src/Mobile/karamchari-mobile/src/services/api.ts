import axios from "axios";

// Note: Replace with your actual backend URL. 
// For local development with Android Emulator, use http://10.0.2.2:5000
// For physical devices, use your computer's local IP.
export const api = axios.create({
  baseURL: "http://10.0.2.2:5000", 
});

export const sendPunch = async (payload: {
  employeeId: string;
  timestamp: string;
  lat: number;
  lon: number;
  accuracy: number;
  isOfflineCaptured: boolean;
  clientId: string;
}) => {
  return api.post("/api/mobile/punch", payload);
};
