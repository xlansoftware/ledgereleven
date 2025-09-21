import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { CalendarIcon, X } from "lucide-react";
import { MultiSelect } from "@/components/multi-select";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { type Category, type FilterRequest } from "@/lib/types";
import { formatDateWithoutCurrentYear } from "@/lib/utils";

interface FilterProps {
  categories: Category[];
  users: string[];
  filter: FilterRequest;
  onApply: (filter: FilterRequest) => void;
  onClose?: () => void;
}

export default function Filter({
  onClose,
  onApply,
  filter,
  categories,
  users,
}: FilterProps) {
  const [localFilter, setLocalFilter] = useState<FilterRequest>(filter);

  // Toggle states for each section
  const [dateEnabled, setDateEnabled] = useState(
    !!filter.period && filter.period !== "all"
  );
  const [categoryEnabled, setCategoryEnabled] = useState(
    !!filter.category?.length
  );
  const [userEnabled, setUserEnabled] = useState(!!filter.user?.length);
  const [noteEnabled, setNoteEnabled] = useState(!!filter.note);
  const [valueEnabled, setValueEnabled] = useState(
    !!filter.minValue || !!filter.maxValue
  );

  const handleApply = () => {
    // Clean up the filter object before applying
    const finalFilter: FilterRequest = {
      period: dateEnabled ? localFilter.period || "all" : undefined,
      startDate: dateEnabled ? localFilter.startDate : undefined,
      endDate: dateEnabled ? localFilter.endDate : undefined,
      category: categoryEnabled ? localFilter.category : undefined,
      user: userEnabled ? localFilter.user : undefined,
      note: noteEnabled ? localFilter.note : undefined,
      minValue: valueEnabled ? localFilter.minValue : undefined,
      maxValue: valueEnabled ? localFilter.maxValue : undefined,
    };
    onApply(finalFilter);
  };

  const handleDatePeriodChange = (value: string) => {
    setLocalFilter((prev) => ({
      ...prev,
      period: value as FilterRequest["period"],
      startDate: undefined,
      endDate: undefined,
    }));
  };

  const handleCategoryChange = (selectedIds: number[]) => {
    setLocalFilter((prev) => ({
      ...prev,
      category: selectedIds,
    }));
  };

  const handleUserChange = (selectedUsers: string[]) => {
    setLocalFilter((prev) => ({
      ...prev,
      user: selectedUsers,
    }));
  };

  return (
    <div className="flex flex-col gap-4 px-4 pb-4 w-full max-w-md mx-auto">
      {/* Date Filter Section */}
      <div className="space-y-2 w-full">
        <div className="flex items-center gap-2 items-center">
          <Switch
            id="dateEnabled"
            checked={dateEnabled}
            onCheckedChange={setDateEnabled}
          />
          {dateEnabled ? (
            <div className="flex flex-row gap-2 items-center w-full">
              <Label htmlFor="dateEnabled">Period</Label>
              {/* <div className="flex-1"></div> */}
              <Select
                value={localFilter.period || "all"}
                onValueChange={handleDatePeriodChange}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select period" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All time</SelectItem>
                  <SelectItem value="today">Today</SelectItem>
                  <SelectItem value="thisWeek">This week</SelectItem>
                  <SelectItem value="thisMonth">This month</SelectItem>
                  <SelectItem value="thisYear">This year</SelectItem>
                  <SelectItem value="custom">Custom range</SelectItem>
                </SelectContent>
              </Select>
            </div>
          ) : (
            <Label htmlFor="dateEnabled">Any date</Label>
          )}
        </div>

        {dateEnabled && localFilter.period === "custom" && (
          <div className="space-y-3">
            {localFilter.period === "custom" && (
              <div className="grid grid-cols-2 gap-2">
                <Popover>
                  <PopoverTrigger asChild>
                    <Button
                      variant={"outline"}
                      className="w-full justify-start text-left font-normal"
                    >
                      <CalendarIcon className="mr-2 h-4 w-4" />
                      {localFilter.startDate
                        ? formatDateWithoutCurrentYear(localFilter.startDate)
                        : "Start date"}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0">
                    <Calendar
                      mode="single"
                      selected={
                        localFilter.startDate
                          ? new Date(localFilter.startDate)
                          : undefined
                      }
                      onSelect={(date) =>
                        setLocalFilter((prev) => ({
                          ...prev,
                          startDate: date?.toISOString(),
                        }))
                      }
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>

                <Popover>
                  <PopoverTrigger asChild>
                    <Button
                      variant={"outline"}
                      className="w-full justify-start text-left font-normal"
                    >
                      <CalendarIcon className="mr-2 h-4 w-4" />
                      {localFilter.endDate
                        ? formatDateWithoutCurrentYear(localFilter.endDate)
                        : "End date"}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0">
                    <Calendar
                      mode="single"
                      selected={
                        localFilter.endDate
                          ? new Date(localFilter.endDate)
                          : undefined
                      }
                      onSelect={(date) =>
                        setLocalFilter((prev) => ({
                          ...prev,
                          endDate: date?.toISOString(),
                        }))
                      }
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Category Filter Section */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Switch
            id="categoryEnabled"
            checked={categoryEnabled}
            onCheckedChange={setCategoryEnabled}
          />
          {categoryEnabled ? (
            <Label htmlFor="categoryEnabled">Select categories</Label>
          ) : (
            <Label htmlFor="categoryEnabled">Any category</Label>
          )}
        </div>

        {categoryEnabled && (
          <MultiSelect
            options={categories.map((c) => ({
              value: c.id.toString(),
              label: c.name,
              color: c.color,
            }))}
            selected={localFilter.category?.map(String) || []}
            onChange={(selected) => handleCategoryChange(selected.map(Number))}
            placeholder="Select categories..."
            renderBadge={({ option, onRemove }) => (
              <Badge
                key={option.value}
                onClick={onRemove}
                variant="outline"
                className="mr-1 mb-1 cursor-pointer"
                style={option.color ? { borderColor: option.color } : {}}
              >
                {option.label}
                <div className="ml-1 rounded-full outline-none focus:ring-2 focus:ring-ring">
                  <X className="h-3 w-3" />
                </div>
              </Badge>
            )}
          />
        )}
      </div>

      {/* User Filter Section */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Switch
            id="userEnabled"
            checked={userEnabled}
            onCheckedChange={setUserEnabled}
          />
          {userEnabled ? (
            <Label htmlFor="userEnabled">Select users</Label>
          ) : (
            <Label htmlFor="userEnabled">Any user</Label>
          )}
        </div>

        {userEnabled && (
          <MultiSelect
            options={users.map((u) => ({
              value: u,
              label: u,
            }))}
            selected={localFilter.user || []}
            onChange={handleUserChange}
            placeholder="Select users..."
          />
        )}
      </div>

      {/* Note Filter Section */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Switch checked={noteEnabled} onCheckedChange={setNoteEnabled} />
          {noteEnabled ? (
            <Label>Search in notes</Label>
          ) : (
            <Label>Any note content</Label>
          )}
        </div>

        {noteEnabled && (
          <Input
            placeholder="Search in notes..."
            value={localFilter.note || ""}
            onChange={(e) =>
              setLocalFilter((prev) => ({
                ...prev,
                note: e.target.value,
              }))
            }
          />
        )}
      </div>

      {/* Value Range Filter Section */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Switch
            id="valueEnabled"
            checked={valueEnabled}
            onCheckedChange={setValueEnabled}
          />
          {valueEnabled ? (
            <Label htmlFor="valueEnabled">Filter by value range</Label>
          ) : (
            <Label htmlFor="valueEnabled">Any value</Label>
          )}
        </div>

        {valueEnabled && (
          <div className="grid grid-cols-2 gap-2">
            <Input
              type="number"
              placeholder="Min value"
              value={localFilter.minValue || ""}
              onChange={(e) =>
                setLocalFilter((prev) => ({
                  ...prev,
                  minValue: e.target.value ? Number(e.target.value) : undefined,
                }))
              }
            />
            <Input
              type="number"
              placeholder="Max value"
              value={localFilter.maxValue || ""}
              onChange={(e) =>
                setLocalFilter((prev) => ({
                  ...prev,
                  maxValue: e.target.value ? Number(e.target.value) : undefined,
                }))
              }
            />
          </div>
        )}
      </div>

      <div className="flex gap-2">
        {onClose && (
          <Button variant="outline" className="flex-1" onClick={onClose}>
            Cancel
          </Button>
        )}
        <Button className="flex-1" onClick={handleApply}>
          Apply Filters
        </Button>
      </div>
    </div>
  );
}
