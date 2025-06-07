"use client";

import React from "react";
import {
  Label,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { formatCurrency } from "@/lib/utils";
import { Category } from "@/lib/types";
import { getIcon } from "@/lib/getIcon";

export interface InsightComponentProps {
  data: Record<string, number>;
  title: string;
  categories: Record<string, Category>;
}

export const InsightComponent: React.FC<InsightComponentProps> = ({
  data,
  title,
  categories,
}) => {
  // Prepare data for the Pie chart
  const chartData = Object.entries(data).map(([name, value]) => ({
    name,
    value,
    color: categories[name]?.color || "#8884d8",
    icon: getIcon(categories[name]?.icon),
  }));

  // Calculate the total value
  const totalValue = chartData.reduce((sum, entry) => sum + entry.value, 0);

  // Define radii for the donut shape
//   const outerRadius = 80; // Adjust as needed
  const innerRadius = 60; // Adjust to control the donut thickness

  const charTitle: Record<string, string> = {
    today: "Today",
    yesterday: "Yesterday",
    thisWeek: "This Week",
    lastWeek: "Last Week",
    thisMonth: "This Month",
    lastMonth: "Last Month",
    thisYear: "This Year",
    lastYear: "Last Year",
    total: "All Time",
  };

  return (
    <div>
      {chartData.length === 0 ? (
        <p className="text-muted-foreground text-center">&nbsp;</p>
      ) : (
        <div className="relative h-64 w-full flex justify-center items-center">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Tooltip
                formatter={(value: number, name: string) => [`${value}`, name]} // Basic tooltip formatting
              />
              <Pie
                isAnimationActive={false}
                label={({ cx, cy, midAngle, outerRadius, icon }) => {
                  const RADIAN = Math.PI / 180;
                  const x =
                    cx + (outerRadius + 20) * Math.cos(-midAngle * RADIAN);
                  const y =
                    cy + (outerRadius + 20) * Math.sin(-midAngle * RADIAN);

                  const Icon = icon as React.FunctionComponent<
                    React.SVGProps<SVGSVGElement>
                  >;
                  return <Icon x={x - 10} y={y - 10} width="20" height="20" />;
                }}
                labelLine={false}
                data={chartData}
                dataKey="value"
                nameKey="name"
                cx="50%" // Center horizontally
                cy="50%" // Center vertically
                //   outerRadius={outerRadius}
                innerRadius={innerRadius}
                paddingAngle={5} // Adds spacing between segments
                fill="#8884d8" // Default fill, overridden by Cells
              >
                {chartData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={entry.color} />
                ))}
                <Label
                  content={({ viewBox }) => {
                    if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                      return (
                        <text
                          x={viewBox.cx}
                          y={viewBox.cy}
                          textAnchor="middle"
                          dominantBaseline="middle"
                        >
                          <tspan
                            x={viewBox.cx}
                            y={viewBox.cy}
                            className="fill-foreground text-3xl font-bold"
                          >
                            {formatCurrency(totalValue, 0)}
                          </tspan>
                          <tspan
                            x={viewBox.cx}
                            y={(viewBox.cy || 0) + 24}
                            className="fill-muted-foreground"
                          >
                            {charTitle[title]}
                          </tspan>
                        </text>
                      );
                    }
                  }}
                />
              </Pie>
            </PieChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
};
