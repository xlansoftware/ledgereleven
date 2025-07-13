import { accountInfo, login } from "@/api";
import { useSpaceStore } from "@/lib/store-space";
import { useEffect, useState } from "react";
import { useBookStore } from "@/lib/store-book";

export default function useUser() {
  const [user, setUser] = useState<string | null>(null);
  const { loadSpaces } = useSpaceStore();
  const { openBook } = useBookStore();

  useEffect(() => {
    accountInfo().then(({ name }) => {
      setUser(name);
    }).catch((error) => {
      console.error("Error fetching account info:", error);
      setUser(null);
    });
  }, []);

  const callLogin = async (username: string, password: string): Promise<void> => {
    await login(username, password);
    const current = await loadSpaces();
    if (current) {
      await openBook(current.id!);
    }
    const { name } = await accountInfo();
    setUser(name);
  };

  return {
    name: user,
    login: callLogin,
  };
}
