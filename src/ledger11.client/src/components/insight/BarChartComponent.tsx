import { Category } from "@/lib/types";
import { HistoryRecord } from "@/components/insight/HistoryComponent";
import {
  ChartContainer,
} from "@/components/ui/chart";
import {
  Bar,
  BarChart,
  CartesianGrid,
  XAxis,
  ResponsiveContainer,
  LabelList,
} from "recharts";


interface BarChartComponentProps {
  data: HistoryRecord[];
  title: string;
  categories: Record<string, Category>;
}

function getDayName(dateString: string, locale: string = "en-US"): string {
  const date = new Date(dateString);
  return date.toLocaleDateString(locale, { weekday: "short" });
}

export default function BarChartComponent({
  data,
}: BarChartComponentProps) {
  return (
    <ChartContainer config={{}}>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={data}>
          <CartesianGrid vertical={false} />
          <XAxis
            dataKey="date"
            tickLine={false}
            tickMargin={10}
            axisLine={false}
            tickFormatter={(value) => getDayName(value)}
          />
          <Bar
            dataKey={"value"}
            fill={"#8884d8"}
            isAnimationActive={false}
            radius={4}
          >
            <LabelList
              position="top"
              offset={12}
              className="fill-foreground"
              fontSize={12}
            />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
}
