import { motion } from "framer-motion";
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  Tooltip,
} from "recharts";

type ChartItem = {
  label: string;
  value: number;
};

type Props = {
  data: ChartItem[];
  title: string;
};

export default function RevolutChartComponent({ data, title }: Props) {
  const latestValue = data[data.length - 1]?.value ?? 0;
  const maxValue = Math.max(...data.map((d) => d.value), 0);

  return (
    <motion.div
      initial={{ opacity: 0, y: 30 }}
      animate={{ opacity: 1, y: 0 }}
      className="w-full max-w-md rounded-3xl p-5 bg-gradient-to-br from-[#1a1c2e] to-[#121322] shadow-2xl"
    >
      {/* Header */}
      <div className="mb-4">
        <p className="text-sm text-gray-400">{title}</p>
        <div className="flex items-end gap-2">
          <h1 className="text-3xl font-semibold">
            {new Intl.NumberFormat("fr-FR").format(Math.round(latestValue))}
          </h1>
        </div>
      </div>

      {/* Chart */}
      <div className="h-40">  
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data}>
            <defs>
              <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#34d399" stopOpacity={0.4} />
                <stop offset="100%" stopColor="#34d399" stopOpacity={0} />
              </linearGradient>
            </defs>

            <XAxis
              dataKey="label"
              axisLine={false}
              tickLine={false}
              tick={{ fill: "#6b7280", fontSize: 12 }}
            />

            <Tooltip
              contentStyle={{
                backgroundColor: "#111",
                border: "none",
                borderRadius: "10px",
                color: "#fff",
              }}
            />

            <Area
              type="monotone"
              dataKey="value"
              stroke="#34d399"
              strokeWidth={2.5}
              fill="url(#colorValue)"
              dot={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      {/* Top value hint */}
      <div className="flex justify-end text-xs text-gray-400 mt-1">
        {maxValue.toFixed(0)} €
      </div>
    </motion.div>
  );
}