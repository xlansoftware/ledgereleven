import { BrowserRouter as Router } from "react-router-dom";
import AppLayout from "@/components/app-layout";

export default function Home() {

  return (
    <Router basename={import.meta.env.BASE_URL}>
      <AppLayout />
    </Router>
  );
}
