import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  // We hit the .NET BFF directly via NEXT_PUBLIC_API_BASE_URL; no rewrites needed.
  // If we later want to proxy /api -> http://localhost:5000 to avoid CORS,
  // do it here with an async rewrites() function — but the BFF's dev CORS
  // policy already handles localhost:3000 cleanly.
};

export default nextConfig;
