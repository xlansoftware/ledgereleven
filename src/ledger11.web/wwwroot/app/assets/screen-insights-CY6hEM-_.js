const __vite__mapDeps=(i,m=__vite__mapDeps,d=(m.f||(m.f=["assets/TotalByPeriodByCategoryComponent-2Zah-4J5.js","assets/index-C6kYvDVg.js","assets/index-BbAWtvYr.css","assets/InsightComponent-DC8xkgT9.js","assets/generateCategoricalChart-DkIKrFhc.js","assets/PieChart-OLojmHD3.js","assets/HistoryComponent-C8PclTjI.js","assets/BarChart-ctKkiio9.js","assets/PerPeriodComponent-B1qS2MEZ.js"])))=>i.map(i=>d[i]);
import{c,r as s,j as e,a6 as l,a7 as d,a8 as n,a9 as o}from"./index-C6kYvDVg.js";/**
 * @license lucide-react v0.488.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const p=[["path",{d:"M3 3v16a2 2 0 0 0 2 2h16",key:"c24i48"}],["path",{d:"M18 17V9",key:"2bz60n"}],["path",{d:"M13 17V5",key:"1frdt8"}],["path",{d:"M8 17v-3",key:"17ska0"}]],h=c("chart-column",p);/**
 * @license lucide-react v0.488.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const m=[["path",{d:"M21 12c.552 0 1.005-.449.95-.998a10 10 0 0 0-8.953-8.951c-.55-.055-.998.398-.998.95v8a1 1 0 0 0 1 1z",key:"pzmjnu"}],["path",{d:"M21.21 15.89A10 10 0 1 1 8 2.83",key:"k2fpak"}]],_=c("chart-pie",m),u=s.lazy(()=>o(()=>import("./TotalByPeriodByCategoryComponent-2Zah-4J5.js"),__vite__mapDeps([0,1,2,3,4,5]))),x=s.lazy(()=>o(()=>import("./HistoryComponent-C8PclTjI.js"),__vite__mapDeps([6,1,2,4,7]))),t=s.lazy(()=>o(()=>import("./PerPeriodComponent-B1qS2MEZ.js"),__vite__mapDeps([8,1,2,3,4,5])));function y(){const[a,r]=s.useState("total");return e.jsxs(e.Fragment,{children:[e.jsx("div",{className:"container flex items-center justify-center w-full",children:e.jsx(l,{value:a,className:"pr-4",onValueChange:i=>r(i),children:e.jsxs(d,{children:[e.jsx(n,{value:"total",className:"m-4",children:e.jsx(_,{className:"w-6 h-6"})}),e.jsx(n,{value:"history",className:"m-4",children:e.jsx(h,{className:"w-6 h-6"})})]})})}),e.jsxs(s.Suspense,{children:[a==="history"&&e.jsx(x,{}),a==="total"&&e.jsx(u,{setTab:r}),a==="per-month"&&e.jsx(t,{period:"month"}),a==="per-week"&&e.jsx(t,{period:"week"}),a==="per-day"&&e.jsx(t,{period:"day"})]})]})}export{y as default};
