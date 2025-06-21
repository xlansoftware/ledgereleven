import { fetchWithAuth } from "@/api";
import { useEffect, useState } from "react";

interface VersionResponse {
  version: string;
  commit: string;
  built: string;
}

export default function useVersion() {
  const [appVersion, setAppVersion] = useState<VersionResponse | null>(null);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    fetchWithAuth("/api/version")
      .then(async (res) => {
        if (!res.ok) {
          throw new Error(`HTTP error ${res.status}`);
        }
        const data = await res.json();
        setAppVersion(data);
      })
      .catch((err) => {
        setError(err);
      });
  }, []);

  return {
    appVersion,
    error,
  };
}
