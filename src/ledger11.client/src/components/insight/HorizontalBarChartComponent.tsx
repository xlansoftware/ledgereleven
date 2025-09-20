import { type Category } from "@/lib/types";
import { type HistoryRecord } from "@/components/insight/HistoryComponent";
import {
  type ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
} from "@/components/ui/chart";
import {
  Bar,
  BarChart,
  CartesianGrid,
  XAxis,
  ResponsiveContainer,
  LabelList,
  YAxis,
  Tooltip,
} from "recharts";
// import { formatCurrency } from "@/lib/utils";

interface HorizontalBarChartComponentProps {
  data: HistoryRecord[];
  title: string;
  categories: Record<string, Category>;
}

// function getDayName(dateString: string, locale: string = "en-US"): string {
//   const date = new Date(dateString);
//   return date.toLocaleDateString(locale, { weekday: "short" });
// }

const chartConfig = {
  desktop: {
    label: "Desktop",
    color: "var(--chart-2)",
  },
  mobile: {
    label: "Mobile",
    color: "var(--chart-2)",
  },
  label: {
    color: "var(--background)",
  },
} satisfies ChartConfig;

export default function HorizontalBarChartComponent({
  data,
}: HorizontalBarChartComponentProps) {
  return (
    <ChartContainer config={chartConfig}>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart
          accessibilityLayer
          data={data}
          layout="vertical"
          margin={{
            right: 16,
          }}
        >
          <Tooltip
            formatter={(value: number, name: string) => [`${value}`, name]}
          />

          <CartesianGrid horizontal={false} />
          <YAxis
            dataKey="date"
            type="category"
            tickLine={false}
            tickMargin={10}
            axisLine={false}
            //   tickFormatter={(value) => value.slice(0, 3)}
            hide
          />
          <XAxis dataKey="value" type="number" hide />
          <ChartTooltip
            cursor={false}
            content={<ChartTooltipContent indicator="line" />}
          />
          <Bar
            dataKey="value"
            layout="vertical"
            fill="var(--color-desktop)"
            isAnimationActive={false}
            radius={4}
          >
            <LabelList
              dataKey="date"
              position="insideLeft"
              offset={8}
              className="fill-(--color-label)"
              fontSize={12}
            />
            <LabelList
              dataKey="value"
              position="right"
              offset={8}
              className="fill-foreground"
              fontSize={12}
            />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
}
