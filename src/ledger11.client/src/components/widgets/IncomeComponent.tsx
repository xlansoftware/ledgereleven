"use client";

import React from "react";
import { BarChart, Bar, XAxis, CartesianGrid, YAxis, BarProps, LabelList } from "recharts";
import { formatCurrency } from "@/lib/utils";
import { Category } from "@/lib/types";
import { getIcon } from "@/lib/getIcon";
import { ChartContainer, ChartTooltip, ChartTooltipContent } from "../ui/chart";

export interface IncomeComponentProps {
  data: Record<string, number>;
  title: string;
  categories: Record<string, Category>;
}

const R = 12; // Income bar radius

export const IncomeComponent: React.FC<IncomeComponentProps> = ({
  data,
  categories,
}) => {
  const totalValue = Object.entries(data).reduce((sum, [, value]) => sum + value, 0);

  const chartConfig = Object.entries(data).reduce((acc, [name]) => {
    return {
      ...acc,
      [name]: {
        label: name,
        color: categories[name]?.color || "#8884d8",
        icon: getIcon(categories[name]?.icon),
      }
    }
  }, {});

  const dataWithTotal = {
    ...data,
    total: `Income ${totalValue}`
  };

  return (
    <div>
      {totalValue === 0 ? (
        <p className="text-muted-foreground text-center">&nbsp;</p>
      ) : (
        <div className="relative h-30 w-20 flex flex-col items-center">
          <ChartContainer config={chartConfig} className="min-h-64 w-full">
            <BarChart 
              accessibilityLayer 
              data={[dataWithTotal]}
              margin={{ top: 20, right: 0, left: 0, bottom: 60 }}>
              <CartesianGrid vertical={false} />
              <YAxis 
                domain={[0, totalValue]}
                hide={true}
              />
              <XAxis
                dataKey="total"
                tickLine={false}
                tickMargin={10}
                axisLine={false}
                hide={true}
              />
              <ChartTooltip content={<ChartTooltipContent />} />
              {Object.entries(data).map(([name], index, arr) => <Bar
                isAnimationActive={false}
                dataKey={name}
                stackId="a"
                label={name}
                fill={categories[name]?.color || "#8884d8"}
                radius={index === 0 ? [0, 0, R, R] : (index === arr.length - 1 ? [R, R, 0, 0] : [0, 0, 0, 0])}
                shape={(props: BarProps ) => <CustomBarWithIcon {...props} icon={categories[name]?.icon} value={data[name]} />}
                key={name}
              >
                <LabelList dataKey={name} content={({ value }) => value} />
              </Bar>)}
            </BarChart>
          </ChartContainer>
          <div className="mt-[-3em] flex flex-col items-center">
            <span className="font-bold text-foreground text-l">{formatCurrency(totalValue, 0)}</span>
            <span className="text-xs text-muted-foreground">Income</span>
          </div>
        </div>
      )}
    </div>
  );
};

const CustomBarWithIcon = ({
  x,
  y,
  width,
  height,
  fill,
  icon,
}: BarProps & {
  value: number
  icon?: string
}) => {

  const Icon = getIcon(icon) as React.FunctionComponent<
                      React.SVGProps<SVGSVGElement>
                    >;
  return (
    <g className="relative overflow-visible">
      <rect x={x} y={y} width={width} height={height} fill={fill} />
      {height && height > 20 && (
        <foreignObject x={x} y={Number(y) + height / 2 - 10} width={width} height={height}>
          <div className="flex items-center flex-col w-full">
            {/* <span className="fill-foreground text-l font-bold">{formatCurrency(Number(value), 0)}</span> */}
            <Icon />
          </div>
        </foreignObject>
      )}
    </g>
  );
};
