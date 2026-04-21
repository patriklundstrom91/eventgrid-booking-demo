import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";

export function useSignalR(onEventReceived, negotiateUrl = "http://localhost:7071/api/negotiate") {
  const connectionRef = useRef(null);

  useEffect(() => {
    let stopped = false;

    async function start() {
      try {
        console.log("[SignalR] Fetching negotiate from", negotiateUrl);
        const res = await fetch(negotiateUrl, { credentials: "include" });
        console.log("[SignalR] Negotiate status", res.status);
        if (!res.ok) throw new Error(`Negotiate failed: ${res.status}`);
        const payload = await res.json();
        console.log("[SignalR] Negotiate payload:", payload);

        const hubUrl = payload?.Url ?? payload?.url;
        const token = payload?.AccessToken ?? payload?.accessToken;
        if (!hubUrl) throw new Error("Missing hubUrl in negotiate response");

        const connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl, { accessTokenFactory: () => token ?? "" })
          .withAutomaticReconnect()
          .configureLogging(signalR.LogLevel.Information)
          .build();

        connection.onreconnecting((err) => console.warn("[SignalR] reconnecting", err));
        connection.onreconnected((id) => console.log("[SignalR] reconnected, id:", id));
        connection.onclose((err) => console.warn("[SignalR] closed", err));

        connection.on("event", (evt) => {
          console.log("[SignalR] event received raw:", evt);
          try { onEventReceived(evt); } catch (e) { console.error("[SignalR] onEventReceived error:", e); }
        });

        await connection.start();
        if (stopped) { await connection.stop(); return; }
        console.log("[SignalR] connected, connectionId:", connection.connectionId);
        connectionRef.current = connection;
      } catch (err) {
        console.error("[SignalR] start error:", err);
      }
    }

    start();

    return () => {
      stopped = true;
      const conn = connectionRef.current;
      if (conn) conn.stop().catch(e => console.warn("[SignalR] stop error", e));
    };
  }, [onEventReceived, negotiateUrl]);

  return connectionRef;
}
