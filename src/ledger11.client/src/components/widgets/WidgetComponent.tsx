// components/WidgetComponent.tsx
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  Legend,
  ResponsiveContainer,
} from "recharts";
import { type WidgetParams } from "@/types/widget";
export const WidgetComponent: React.FC<{ params: WidgetParams }> = ({
  params,
}) => {
  const { chartType, title, data, color = "#8884d8" } = params;

  const renderChart = () => {
    switch (chartType) {
      case "Line":
      case "Bar": {
        const { xAxisKey, yAxisKey } = params;

        const axisKey = Array.isArray(xAxisKey) ? xAxisKey[0] : xAxisKey;
        const dataKeys = Array.isArray(yAxisKey) ? yAxisKey : [yAxisKey];

        const Chart = chartType === "Line" ? LineChart : BarChart;
        const ChartElement = dataKeys.map((key) =>
          chartType === "Line" ? (
            <Line
              isAnimationActive={false}
              type="monotone"
              dataKey={key}
              stroke={color}
            />
          ) : (
            <Bar
              isAnimationActive={false}
              dataKey={key}
              fill={color}
              radius={4}
            />
          )
        );

        return (
          <ResponsiveContainer width="100%" height={300}>
            <Chart data={data}>
              {params.showCartesianGrid && (
                <CartesianGrid strokeDasharray="3 3" />
              )}
              <XAxis dataKey={axisKey} />
              {params.showYAxis && <YAxis dataKey={dataKeys[0]} />}
              <Tooltip
                formatter={(value: number, name: string) => [`${value}`, name]}
              />
              {params.showLegend && <Legend />}
              {ChartElement}
            </Chart>
          </ResponsiveContainer>
        );
      }

      case "Pie": {
        const { pieDataKeys } = params;
        if (!pieDataKeys) return <p>Pie chart requires nameKey and valueKey</p>;

        return (
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={data}
                dataKey={pieDataKeys.valueKey}
                nameKey={pieDataKeys.nameKey}
                outerRadius={100}
                fill={color}
                label
              >
                {data.map((_, index) => (
                  <Cell key={`cell-${index}`} fill={color} />
                ))}
              </Pie>
              <Tooltip />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        );
      }

      default:
        return <p>Unsupported chart type</p>;
    }
  };

  return (
    <Card className="rounded-2xl shadow-md p-4">
      {title && (
        <CardHeader>
          <CardTitle>{title}</CardTitle>
        </CardHeader>
      )}
      <CardContent>{renderChart()}</CardContent>
    </Card>
  );
};
