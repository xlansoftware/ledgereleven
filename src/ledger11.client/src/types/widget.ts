// types/widget.ts
export type ChartType = 'Line' | 'Bar' | 'Pie';

export interface WidgetParamsBase {
  chartType: ChartType;
  title?: string;
  data: Record<string, unknown>[];
  color?: string;
  showLegend?: boolean;
}

export interface XYWidgetParams extends WidgetParamsBase {
  chartType: 'Line' | 'Bar';
  xAxisKey: string | string[];
  yAxisKey: string | string[];
  showYAxis?: boolean;
  showCartesianGrid?: boolean;
}

export interface PieWidgetParams extends WidgetParamsBase {
  chartType: 'Pie';
  pieDataKeys: {
    nameKey: string;
    valueKey: string;
  };
}

export type WidgetParams = XYWidgetParams | PieWidgetParams;
