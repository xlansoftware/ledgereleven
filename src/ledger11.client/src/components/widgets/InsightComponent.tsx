"use client";

import React, { ReactNode } from "react";
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
  altData?: Record<string, number>;
  title: string;
  categories: Record<string, Category>;
  children?: ReactNode;
}

export const InsightComponent: React.FC<InsightComponentProps> = ({
  data,
  altData,
  title,
  categories,
  children,
}) => {
  // Prepare data for the Pie chart
  const chartData = Object.entries(data).map(([name, value]) => ({
    name,
    value,
    color: categories[name]?.color || "#8884d8",
    icon: getIcon(categories[name]?.icon),
  }));

  const altChartData =
    altData &&
    Object.entries(altData).map(([name, value]) => ({
      name,
      value,
      color: categories[name]?.color || "#8884d8",
      icon: getIcon(categories[name]?.icon),
    }));

  // Calculate the total value
  const totalValue = chartData.reduce((sum, entry) => sum + entry.value, 0);
  const altTotalValue = altChartData
    ? altChartData.reduce((sum, entry) => sum + entry.value, 0)
    : 0;

  const { startAngle, endAngle, altStartAngle, altEndAngle } = computeAngles(
    totalValue,
    altTotalValue
  );

  // Define radii for the donut shape
  //   const outerRadius = 80; // Adjust as needed
  const innerRadius = altTotalValue == 0 ? 60 : 70; // Adjust to control the donut thickness
  const altInnnerRadius = 60;
  const altSize = 20;

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

  const DoubleTitle = ({
    viewBox,
  }: {
    viewBox: { cx: number; cy: number };
  }) => (
    <text
      x={viewBox.cx}
      y={viewBox.cy}
      textAnchor="middle"
      dominantBaseline="middle"
    >
      <tspan
        x={viewBox.cx}
        y={(viewBox.cy || 0) - 12}
        className="fill-foreground text-3xl font-bold"
      >
        {formatCurrency(totalValue, 0)}
      </tspan>
      <tspan
        x={viewBox.cx}
        y={(viewBox.cy || 0) + 12}
        className="fill-muted-foreground text-l font-bold"
      >
        ({formatCurrency(altTotalValue, 0)})
      </tspan>
      <tspan
        x={viewBox.cx}
        y={(viewBox.cy || 0) + 36}
        className="fill-muted-foreground"
      >
        {charTitle[title] ?? title}
      </tspan>
    </text>
  );

  const Title = ({ viewBox }: { viewBox: { cx: number; cy: number } }) => (
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
        {charTitle[title] ?? title}
      </tspan>
    </text>
  );

  const cellLabel = ({ cx, cy, midAngle, outerRadius, icon }: { cx: number, cy: number, midAngle: number, outerRadius: number, icon: unknown }) => {
    const RADIAN = Math.PI / 180;
    const x = cx + (outerRadius + 20) * Math.cos(-midAngle * RADIAN);
    const y = cy + (outerRadius + 20) * Math.sin(-midAngle * RADIAN);

    const Icon = icon as React.FunctionComponent<React.SVGProps<SVGSVGElement>>;
    return <Icon x={x - 10} y={y - 10} width="20" height="20" />;
  };

  return (
    <div className="w-full">
      {chartData.length === 0 ? (
        <p className="text-muted-foreground text-center">&nbsp;</p>
      ) : (
        <div className="relative h-64 w-full flex flex-row ">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart margin={{ top: 0, right: 0, left: 0, bottom: 0 }}>
              <Tooltip
                formatter={(value: number, name: string) => [`${value}`, name]} // Basic tooltip formatting
              />

              {altChartData && (
                <Pie
                  isAnimationActive={false}
                  label={cellLabel}
                  data={altChartData}
                  dataKey="value"
                  nameKey="name"
                  startAngle={altStartAngle}
                  endAngle={altEndAngle}
                  // cx="50%"
                  // cy="50%"
                  outerRadius={altInnnerRadius + altSize}
                  innerRadius={altInnnerRadius}
                  paddingAngle={5}
                >
                  {altChartData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
              )}

              <Pie
                isAnimationActive={false}
                label={cellLabel}
                labelLine={false}
                data={chartData}
                dataKey="value"
                nameKey="name"
                startAngle={startAngle}
                endAngle={endAngle}
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
                      return altTotalValue === 0 ? (
                        <Title
                          viewBox={
                            viewBox as unknown as { cx: number; cy: number }
                          }
                        />
                      ) : (
                        <DoubleTitle
                          viewBox={
                            viewBox as unknown as { cx: number; cy: number }
                          }
                        />
                      );
                    }
                  }}
                />
              </Pie>
            </PieChart>
          </ResponsiveContainer>
          {children}
        </div>
      )}
    </div>
  );
};

// Compute the chart angles
// the size of the arc is proportional to the proportion value/altValue
function computeAngles(
  value: number,
  altValue: number
): {
  startAngle: number;
  endAngle: number;
  altStartAngle: number;
  altEndAngle: number;
} {
  const totalValue = value + altValue;
  const startAngle = 0;
  const endAngle = (value / totalValue) * 360;
  const altStartAngle = endAngle + 5;
  const altEndAngle = altStartAngle + (altValue / totalValue) * 360 - 10;
  return { startAngle, endAngle, altStartAngle, altEndAngle };
}
