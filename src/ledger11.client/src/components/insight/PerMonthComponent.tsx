import { InsightComponent } from "../widgets/InsightComponent";
import { Category } from "@/lib/types";

// Dummy data for categories, mimicking what useCategoryStore would provide.
const dummyCategories: Record<string, Category> = {
  "Groceries": { id: "1", name: "Groceries", icon: "ShoppingCart", color: "#FF6384" },
  "Transport": { id: "2", name: "Transport", icon: "Car", color: "#36A2EB" },
  "Entertainment": { id: "3", name: "Entertainment", icon: "Film", color: "#FFCE56" },
  "Utilities": { id: "4", name: "Utilities", icon: "Lightbulb", color: "#4BC0C0" },
};

// Dummy data for monthly expenses and income.
const monthlyData = [
  {
    title: "June 2025",
    expense: { "Groceries": 450.75, "Transport": 120.50, "Entertainment": 85.00 },
    income: { "Salary": 3000 },
  },
  {
    title: "May 2025",
    expense: { "Groceries": 480.25, "Transport": 110.00, "Utilities": 150.00, "Entertainment": 200.50 },
    income: { "Salary": 3000, "Bonus": 500 },
  },
  {
    title: "April 2025",
    expense: { "Groceries": 430.00, "Transport": 130.75, "Utilities": 145.00 },
    income: { "Salary": 3000 },
  },
];

export default function PerMonthComponent() {
  return (
    <div className="relative space-y-4">
      {monthlyData.map(({ title, expense, income }) => (
        <div key={title} className="mb-4">
          <InsightComponent
            data={expense}
            altData={income}
            title={title}
            categories={dummyCategories}
          />
        </div>
      ))}
    </div>
  );
}