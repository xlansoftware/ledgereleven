import { accountInfo, login } from "@/api";
import { useCategoryStore } from "@/lib/store-category";
import { useSpaceStore } from "@/lib/store-space";
import { useTransactionStore } from "@/lib/store-transaction";
import { useEffect, useState } from "react";

export default function useUser() {
  const [user, setUser] = useState<string | null>(null);
  const { loadSpaces } = useSpaceStore();
  const { loadCategories } = useCategoryStore();
  const { loadTransactions } = useTransactionStore();

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
    await loadCategories();
    await loadSpaces();
    await loadTransactions(true);
    const { name } = await accountInfo();
    setUser(name);
  };

  return {
    name: user,
    login: callLogin,
  };
}
