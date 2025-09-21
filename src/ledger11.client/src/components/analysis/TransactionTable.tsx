import React from 'react';
import type { DailyCategorySummary } from './dataGroup';

interface TransactionTableProps {
  data: DailyCategorySummary[];
}

const TransactionTable: React.FC<TransactionTableProps> = ({ data }) => {
  // Extract all category names from the first row (assuming consistent structure)
  const getCategoryNames = (): string[] => {
    if (data.length === 0) return [];
    
    const firstRow = data[0];
    // Get all keys except 'date'
    const allCategoryNames = Object.keys(firstRow).filter(key => key !== 'date');

    // Filter categories: keep only those that have at least one non-zero value
    return allCategoryNames.filter(categoryName => {
      return data.some(row => {
        const value = row[categoryName];
        return typeof value === 'number' && value !== 0;
      });
    });
  };

  const categoryNames = getCategoryNames();

  // Format currency (optional helper)
  const formatCurrency = (value: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2
    }).format(value);
  };

  if (data.length === 0) {
    return (
      <div className="p-8 text-center text-gray-500">
        No data to display
      </div>
    );
  }

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
                  Date
                </th>
                {categoryNames.map(categoryName => (
                  <th 
                    key={categoryName}
                    scope="col" 
                    className="px-3 py-3.5 text-right text-sm font-semibold text-gray-900"
                  >
                    {categoryName}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 bg-white">
              {data.map((row, index) => (
                <tr 
                  key={index} 
                  className={index % 2 === 0 ? 'bg-white' : 'bg-gray-50'}
                >
                  <td className="whitespace-nowrap py-4 pl-4 pr-3 text-sm font-medium text-gray-900 sm:pl-6">
                    {row.date}
                  </td>
                  {categoryNames.map(categoryName => (
                    <td 
                      key={categoryName}
                      className="whitespace-nowrap px-3 py-4 text-sm text-right font-mono text-gray-900"
                    >
                      {formatCurrency(row[categoryName] as number)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default TransactionTable;