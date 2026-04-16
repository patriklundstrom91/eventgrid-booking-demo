import "../styles/timeline.css";

export default function EventCard({ event }) {
  const type = event.Type;

  const cssClass =
    type === "BookingValidated"
      ? "event-card validated"
      : type === "BookingProcessed"
      ? "event-card processed"
      : "event-card";

  return (
    <div className={cssClass}>
      <h3>{type}</h3>
      <p><strong>BookingId:</strong> {event.Data.BookingId}</p>
      <p><strong>Customer:</strong> {event.Data.CustomerName}</p>
      <p><strong>Time:</strong> {new Date(event.Timestamp).toLocaleTimeString()}</p>
    </div>
  );
}
