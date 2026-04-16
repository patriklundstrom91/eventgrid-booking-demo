import axios from "axios";

const API_URL = "http://localhost:5251/api/events";

export async function getEvents() {
    const res = await axios.get(API_URL);
    return res.data;
}