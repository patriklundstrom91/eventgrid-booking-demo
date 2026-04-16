import { useEffect, useState } from "react";
import { getEvents } from "../api/eventService";
import EventCard from "./EventCard";

export default function BookingTimeline() {
  const [events, setEvents] = useState([]);

  useEffect(() => {
    const interval = setInterval(async () => {
      try {
        const data = await getEvents();
        setEvents(data);
      } catch (err) {
        console.error("Failed to fetch events", err);
      }
    }, 2000);

    return () => clearInterval(interval);
  }, []);

  return (
    <div className="timeline">
      {events.length === 0 && <p style={{ color: "white" }}>No events yet...</p>}
      {events.map((e, i) => (
        <EventCard key={i} event={e} />
      ))}
    </div>
  );
}
