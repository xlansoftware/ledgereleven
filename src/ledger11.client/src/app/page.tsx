import { useEffect } from "react";
import { BrowserRouter as Router } from "react-router-dom";
import AppLayout from "@/components/app-layout";
import { useSpaceStore } from "@/lib/store-space";
import { useBookStore } from "@/lib/store-book";

export default function Home() {

  const { loadSpaces } = useSpaceStore();
  const { openBook } = useBookStore();

  useEffect(() => {
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
