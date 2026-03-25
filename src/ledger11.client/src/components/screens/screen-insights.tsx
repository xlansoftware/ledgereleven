import { useEffect, useState, useCallback } from "react";
import { fetchWithAuth } from "@/api";
import RevolutChartComponent from "@/components/insight/RevoluteChartComponent";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ChartPieIcon, BarChart3Icon } from "lucide-react";

import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
} from "recharts";

type PerPeriodData = {
  title: string;
  expense: Record<string, number>;
  income: Record<string, number>;
};

const COLORS = ["#34d399", "#60a5fa", "#f472b6", "#facc15", "#a78bfa"];

export default function InsightsScreen() {
  const [mode, setMode] = useState<"charts" | "pie">("charts");

  const [weekly, setWeekly] = useState<PerPeriodData[]>([]);
  const [monthly, setMonthly] = useState<PerPeriodData[]>([]);
  const [yearly, setYearly] = useState<PerPeriodData[]>([]);

  const fetchData = useCallback(async (period: string, setter: any) => {
    const res = await fetchWithAuth(
      `/api/insight/per-period/${period}?start=0&count=10`
    );
    if (res.ok) {
      const data = await res.json();
      setter(data);
    }
  }, []);

  useEffect(() => {
    fetchData("week", setWeekly);
    fetchData("month", setMonthly);
    fetchData("year", setYearly);
  }, [fetchData]);

  // ✅ Number formatter (space separator)
  const format = (num: number) =>
    new Intl.NumberFormat("bg-BG", {
      maximumFractionDigits: 0,
    }).format(num);

  // 🔥 Transform for charts
  const transformChart = (
    data: PerPeriodData[],
    period: "week" | "month" | "year"
  ) => {
    let running = 0;

    return data.map((item, index) => {
      const total = Object.values(item.expense).reduce(
        (s, v) => s + v,
        0
      );

      running += total;

      let label = "";

      if (period === "week") {
        const days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        label = days[index % 7];
      }

      if (period === "month") {
        label = `${index + 1}`;
      }

      if (period === "year") {
        const months = [
          "Jan","Feb","Mar","Apr","May","Jun",
          "Jul","Aug","Sep","Oct","Nov","Dec"
        ];
        label = months[index % 12];
      }

      return {
        label,
        value: Math.round(running),
      };
    });
  };

  // 🔥 Transform for pie charts
  const transformPie = (data: PerPeriodData[]) => {
    const totals: Record<string, number> = {};

    data.forEach((item) => {
      Object.entries(item.expense).forEach(([key, val]) => {
        totals[key] = (totals[key] || 0) + val;
      });
    });

    return Object.entries(totals).map(([name, value]) => ({
      name,
      value,
    }));
  };

  const PieBlock = ({ data }: any) => (
    <div className="w-full max-w-md bg-[#121322] p-4 rounded-3xl">
      <div className="h-48">
        <ResponsiveContainer>
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="name"
              innerRadius={60}
              outerRadius={80}
              paddingAngle={3}
            >
              {data.map((_: any, i: number) => (
                <Cell key={i} fill={COLORS[i % COLORS.length]} />
              ))}
            </Pie>
            <Tooltip formatter={(value: number) => format(value)} />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-[#0c0d1a] p-6 text-white space-y-6">
      {/* Toggle */}
      <div className="flex justify-center">
        <Tabs value={mode} onValueChange={(v) => setMode(v as any)}>
          <TabsList className="bg-neutral-800 rounded-2xl p-1 flex gap-2">
            <TabsTrigger value="charts">
              <BarChart3Icon className="w-5 h-5" />
            </TabsTrigger>
            <TabsTrigger value="pie">
              <ChartPieIcon className="w-5 h-5" />
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      {/* AREA CHARTS */}
      {mode === "charts" && (
        <div className="flex flex-col items-center gap-6">
          <RevolutChartComponent
            data={transformChart(weekly, "week")}
            title="Spent this week"
          />

          <RevolutChartComponent
            data={transformChart(monthly, "month")}
            title="Spent this month"
          />

          <RevolutChartComponent
            data={transformChart(yearly, "year")}
            title="Spent this year"
          />
        </div>
      )}

      {/* PIE CHARTS */}
      {mode === "pie" && (
        <div className="flex flex-col items-center gap-6">
          <PieBlock data={transformPie(weekly)} />
          <PieBlock data={transformPie(monthly)} />
          <PieBlock data={transformPie(yearly)} />
        </div>
      )}
    </div>
  );
}