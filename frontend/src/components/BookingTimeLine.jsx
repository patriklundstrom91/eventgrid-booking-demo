import React, { useState, useEffect, useCallback, useRef } from "react";
import { useSignalR } from "../hooks/useSignalR";
import { getEvents } from "../api/eventService";
import EventCard from "./EventCard";

function normalizeEvent(raw) {
  const envelopeId = raw?.id ?? raw?.Id ?? raw?.eventId;
  const bookingId = raw?.Data?.BookingId ?? raw?.data?.BookingId ?? raw?.BookingId;
  const type = raw?.Type ?? raw?.type ?? raw?.eventType ?? "Unknown";
  const timestamp = raw?.Timestamp ?? raw?.timestamp ?? raw?.eventTime ?? raw?.CreatedAt ?? new Date().toISOString();
  const id = envelopeId ?? (bookingId ? `${bookingId}-${type}-${timestamp}` : `${type}-${timestamp}`);
  const data = raw?.Data ?? raw?.data ?? raw;
  return { id, bookingId: bookingId ?? null, Type: type, Data: data, Timestamp: timestamp };
}

export default function BookingTimeline() {
  const [events, setEvents] = useState([]);
  const seenIdsRef = useRef(new Set());
  const [error, setError] = useState(null);
  const [lastTimestamp, setLastTimestamp] = useState(null);

  useEffect(() => {
    let mounted = true;
    async function loadInitial() {
      try {
        const data = await getEvents();
        const arr = Array.isArray(data) ? data : [];
        const normalized = arr.map(normalizeEvent);
        const uniqueById = Array.from(new Map(normalized.map(e => [e.id, e])).values());
        uniqueById.forEach(e => e.id && seenIdsRef.current.add(e.id));
        if (!mounted) return;
        setEvents(uniqueById);
        const latest = uniqueById.reduce((acc, cur) => (cur.Timestamp > acc ? cur.Timestamp : acc), "");
        if (latest) setLastTimestamp(latest);
      } catch (err) {
        console.error("Failed to load events:", err);
        setError("Failed to load events");
      }
    }
    loadInitial();
    return () => { mounted = false; };
  }, []);

  const onEventReceived = useCallback((raw) => {
    const e = normalizeEvent(raw);
    console.log("Incoming event normalized:", e);
    if (e.id && seenIdsRef.current.has(e.id)) {
      console.log("Ignored duplicate id:", e.id);
      return;
    }
    if (e.id) seenIdsRef.current.add(e.id);
    setEvents(prev => {
      if (prev.some(x => x.id === e.id)) return prev;
      return [...prev, e];
    });
    setLastTimestamp(prev => (e.Timestamp > (prev ?? "") ? e.Timestamp : prev));
  }, []);

  const connectionRef = useSignalR(onEventReceived, "http://localhost:7071/api/negotiate");

  useEffect(() => {
    let mounted = true;
    async function fetchMissed() {
      try {
        const conn = connectionRef.current;
        if (!conn || !conn.connectionId) return;
        if (!lastTimestamp) return;
        const url = `http://localhost:5251/api/events?since=${encodeURIComponent(lastTimestamp)}`;
        const res = await fetch(url);
        if (!res.ok) return;
        const missed = await res.json();
        if (!mounted) return;
        const normalized = (Array.isArray(missed) ? missed : []).map(normalizeEvent);
        normalized.forEach(e => {
          if (e.id && !seenIdsRef.current.has(e.id)) {
            seenIdsRef.current.add(e.id);
            setEvents(prev => prev.some(x => x.id === e.id) ? prev : [...prev, e]);
          }
        });
      } catch (err) {
        console.warn("Failed to fetch missed events:", err);
      }
    }
    fetchMissed();
    return () => { mounted = false; };
  }, [connectionRef, lastTimestamp]);

  return (
    <div className="timeline">
      {error && <p style={{ color: "salmon" }}>{error}</p>}
      {events.length === 0 && <p style={{ color: "white" }}>No events yet...</p>}
      {events.map(e => <EventCard key={e.id} event={e} />)}
    </div>
  );
}