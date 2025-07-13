import { Category, Receipt, Transaction } from "./lib/types";

export async function fetchWithAuth(url: string, options = {}, navigateToStartOn401: boolean = true) {
  const res = await fetch(url, {
    headers: {
      "X-Requested-With": "XMLHttpRequest",
    },
    credentials: "include", // Important: send cookies
    ...options,
  });

  if (res.status === 401) {
    console.log("Unauthorized. Redirecting to login...");
    if (navigateToStartOn401) {
      window.location.href = "/start";
    } else {
      throw new Error("Unauthorized");
    }
  }

  if (!res.ok) {
    const err = await res.text();
    throw new Error(err || `Error ${res.status}`);
  }

  return res;
}

export async function accountInfo() {
  const response = await fetchWithAuth("/api/account/user");
  if (!response.ok) {
    throw new Error("Failed to fetch account info");
  }
  const data = await response.json();
  return {
    email: data.email,
    name: data.name,
    isLoggedIn: data.isLoggedIn,
    isSyncEnabled: data.isSyncEnabled,
    isSyncing: data.isSyncing,
  };
}

export async function login(username: string, password: string) {
  const response = await fetchWithAuth("/api/account/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  return response.ok;
}

export async function logout() {
  const response = await fetchWithAuth("/api/account/logout", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  return response.ok;
}

// send the request of the user in a natural language
// and get the decoded purchase records
// e.g. "three eggs 1.20 each, milk 2.50, bread 1.50"
export async function parsePurchaseRecords(
  text: string
): Promise<Partial<Receipt>> {
  // mock response
  // await new Promise((resolve) => setTimeout(resolve, 1000));
  // const mockResponse: Partial<PurchaseRecord>[] = [
  //   { description: "eggs", totalAmount: 3.6, amount: 1.2, quantity: 3 },
  //   { description: "milk", totalAmount: 2.5 },
  //   { description: "bread", totalAmount: 1.5 },
  //   { description: "bread", totalAmount: 1.5 },
  //   { description: "bread", totalAmount: 1.5 },
  //   { description: "bread", totalAmount: 1.5 },
  //   { description: "bread", totalAmount: 1.5 },
  // ];

  // return mockResponse;

  const response = await fetchWithAuth("/api/purchases/parse", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ query: text }),
  });

  if (!response.ok) {
    throw new Error("Failed to parse purchase records");
  }

  return await response.json();
}

export async function scanReceipt(image: File): Promise<Partial<Receipt>> {
  const formData = new FormData();
  formData.append("imageFile", image);

  const response = await fetchWithAuth("/api/purchases/scan", {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error("Failed to parse purchase records");
  }

  return await response.json();
}

function parseNumber(n: string | undefined): number | undefined {
  if (!n) return undefined;
  return Number.parseFloat(n);
}

function isCloseEnough(total: number, unit: number, quantity: number): boolean {
  // console.log(`${total} - (${unit} x ${quantity}) = ${total - (unit * quantity)}`);
  return Math.abs(total - (unit * quantity)) < 0.01;
}

export function matchCategory(categories: Category[], name?: string): Category | null {
  if (!name) return null;
  const lowerName = name.toLowerCase();

  return (
    categories.find(c => c.name.toLowerCase() === lowerName) ??
    categories.find(c => c.name.toLowerCase().includes(lowerName)) ??
    null
  );
}

export function receiptToTransaction(categories: Category[], receipt: Receipt): Transaction {
  const result: Transaction = {
    value: parseNumber(receipt.total_paid),
    categoryId: matchCategory(categories, receipt.category)?.id,
    transactionDetails: (receipt.items || []).map((item) => {
      const total_price = parseNumber(item.total_price) || 0;
      const unit_price = parseNumber(item.unit_price) || 0;
      const quantity = parseNumber(item.quantity) || 0;

      const category = matchCategory(categories, item.category);
      return isCloseEnough(total_price, unit_price, quantity)
        ? {
          value: unit_price,
          description: item.name,
          quantity: quantity,
          categoryId: category?.id,
        }
        : {
          value: total_price,
          description: item.name,
          categoryId: category?.id,
        };
    }),
  };

  // if the total do not match the detail, leave only the total
  const sum = result.transactionDetails!.reduce((acc, item) => acc + (item.value || 0), 0);
  if (Math.abs(sum - (result.value || 0)) > 0.01) {
    result.transactionDetails = [];
  }
  return result;
}
