import { useEffect } from "react";
import { BrowserRouter as Router } from "react-router-dom";
import AppLayout from "@/components/app-layout";
import { useSpaceStore } from "@/lib/store-space";
import { useBookStore } from "@/lib/store-book";

export default function Home() {
  const { loadSpaces } = useSpaceStore();
  const { openBook } = useBookStore();

  useEffect(() => {

    if (window.location.href.endsWith("/start")) {
      // /start is an endpoint handled by the backend. (ledger11.web)
      // We can be here only if we are in development mode,
      // when the react app and the backend are running on different ports.
      // If we do not exit here, we will cause endless loop of redirecting to /start...
      return;
    }

    loadSpaces().then((current) => {
      if (!current) {
        console.error("No space found. Please create a space first.");
      } else {
        openBook(current!.id!);
      }
    });
  }, [loadSpaces, openBook]);

  return (
    <Router basename={import.meta.env.BASE_URL}>
      <AppLayout />
    </Router>
  );
}
