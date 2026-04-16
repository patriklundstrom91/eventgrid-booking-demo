import BookingTimeline from "./components/BookingTimeLine";

export default function App() {
  return (
    <div style={{ background: "#121212", minHeight: "100vh", padding: "2rem" }}>
      <h1 style={{ color: "white", textAlign: "center" }}>
        EventGrid Booking Flow
      </h1>
      <BookingTimeline />
    </div>
  );
}