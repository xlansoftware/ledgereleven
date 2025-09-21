import React from "react";
import { getSortedCategories, type TimeframeAnalysis } from "./dataGroup";
import type { Category } from "@/lib/types";

interface CategoryTableProps {
  analysis: TimeframeAnalysis;
  categories: Category[];
}

// Helper function to format currency
const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    useGrouping: true, // This adds thousand separators (commas)
  }).format(value);
};

const CategoryTable: React.FC<CategoryTableProps> = ({
  analysis,
  categories,
}) => {
  const sortedCategories = getSortedCategories(analysis).filter(
    (row) => row.stats.total > 0
  );

  if (sortedCategories.length === 0) {
    return (
      <div className="p-8 text-center text-gray-500 bg-white rounded-lg shadow">
        No category data available
      </div>
    );
  }

  const categoryMap = new Map<string, Category>();
  categories.forEach((category) => {
    categoryMap.set(category.name, category);
  });

  const total = sortedCategories.reduce((sum, cat) => sum + cat.stats.total, 0);
  const dailyAverage = sortedCategories.reduce(
    (sum, cat) => sum + cat.stats.dailyAverage,
    0
  );
  const weeklyAverage = sortedCategories.reduce(
    (sum, cat) => sum + cat.stats.weeklyAverage,
    0
  );
  const monthlyAverage = sortedCategories.reduce(
    (sum, cat) => sum + cat.stats.monthlyAverage,
    0
  );

  return (
    <div className="overflow-x-auto">
      <div className="inline-block min-w-full align-middle">
        <div className="shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
          <table className="min-w-full divide-y divide-gray-300">
            <thead className="bg-gray-50">
              <tr>
                <th
                  scope="col"
                  className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold text-gray-900 sm:pl-6"
                >
                  Category
                </th>
                <th
                  scope="col"
                  className="px-3 py-3.5 text-right text-sm font-semibold text-gray-900"
                >
                  Total
                </th>
                <th
                  scope="col"
                  className="px-3 py-3.5 text-right text-sm font-semibold text-gray-900"
                >
                  Daily Avg
                </th>
                <th
                  scope="col"
                  className="px-3 py-3.5 text-right text-sm font-semibold text-gray-900"
                >
                  Weekly Avg
                </th>
                <th
                  scope="col"
                  className="px-3 py-3.5 text-right text-sm font-semibold text-gray-900"
                >
                  Monthly Avg
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 bg-white">
              {sortedCategories.map((category, index) => (
                <tr
                  key={category.name}
                  className={index % 2 === 0 ? "bg-white" : "bg-gray-50"}
                >
                  <td className="py-4 pl-0 pr-3 text-sm font-medium text-gray-900 relative">
                    {/* Colored left bar - matches full cell height */}
                    <div
                      className="absolute left-0 top-0 bottom-0 w-3"
                      style={{
                        backgroundColor:
                          categoryMap.get(category.name)?.color || "#3b82f6", // fallback to blue
                      }}
                    ></div>

                    {/* Content wrapper with left padding to avoid overlapping the color bar */}
                    <div className="pl-4">
                      {category.name}
                      <div className="mt-2 w-full bg-gray-200 rounded-full h-2.5">
                        <div
                          className="bg-blue-600 h-2.5 rounded-full transition-all duration-300 ease-out"
                          style={{
                            width:
                              total > 0
                                ? `${(category.stats.total / total) * 100}%`
                                : "0%",
                          }}
                        ></div>
                      </div>
                    </div>
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                    {formatCurrency(category.stats.total)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                    {formatCurrency(category.stats.dailyAverage)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                    {formatCurrency(category.stats.weeklyAverage)}
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                    {formatCurrency(category.stats.monthlyAverage)}
                  </td>
                </tr>
              ))}
              <tr className="bg-blue-50 font-semibold">
                <td className="whitespace-nowrap py-4 pl-4 pr-3 text-sm text-gray-900 sm:pl-6">
                  TOTAL
                </td>
                <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                  {formatCurrency(total)}
                </td>
                <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                  {formatCurrency(dailyAverage)}
                </td>
                <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                  {formatCurrency(weeklyAverage)}
                </td>
                <td className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900">
                  {formatCurrency(monthlyAverage)}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default CategoryTable;
