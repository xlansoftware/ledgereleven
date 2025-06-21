"use client";

import type React from "react";

import { useEffect, useRef, useState } from "react";
import { count, downloadUrl } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Download, Upload, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { PreferredLanguage } from "@/components/PreferredLanguage";
import useUser from "@/hooks/useUser";
import { useNavigate } from "react-router-dom";
import { ThemeToggle } from "../theme-toggle";
import { useTheme } from "../theme-context";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import { useSpaceStore } from "@/lib/store-space";
import { useTransactionStore } from "@/lib/store-transaction";
import { fetchWithAuth } from "@/api";
import { useConfirmDialog } from "../dialog/ConfirmDialogContext";
import useVersion from "@/hooks/useVersion";

export default function Settings() {
  const navigate = useNavigate();
  const { setTheme } = useTheme();
  const { name } = useUser();
  const { current, spaces, setCurrentSpace, loadSpaces } = useSpaceStore();
  const { loadTransactions } = useTransactionStore();
  const { confirm } = useConfirmDialog();

  const [isImporting, setIsImporting] = useState(false);

  const { appVersion } = useVersion();

  useEffect(() => {
    loadSpaces();
  }, [loadSpaces]);

  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleChangeSpace = async (value: string) => {
    await setCurrentSpace(value);
    await loadTransactions(true);
    toast(`Current space changed`);
  };

  const handleLogout = () => {
    window.location.href = "/home/signoutall";
  };

  const handleBackupCsv = () => {
    downloadUrl("/api/backup/export?format=csv");
    toast("Your data has been exported to a CSV file.");
  };

  const handleBackupExcel = () => {
    downloadUrl("/api/backup/export?format=excel");
    toast("Your data has been exported to a Excel file.");
  };

  const handleRestore = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setIsImporting(true); // Start loading

    const formData = new FormData();
    formData.append("dataFile", file);

    try {
      const response = await fetchWithAuth(
        "/api/backup/import?clearExistingData=false",
        {
          method: "POST",
          body: formData,
        }
      );

      if (!response.ok) {
        const errorText = await response.text();
        console.error("Upload failed:", errorText);
        toast.error(`Upload failed: ${errorText}`);
      } else {
        toast.success("File imported successfully!");
      }

      await loadTransactions(true);
    } catch (error) {
      console.error("An error occurred while uploading the file:", error);
      toast.error("An unexpected error occurred. Please try again.");
    } finally {
      setIsImporting(false); // Stop loading
      event.target.value = "";
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  };

  const handleClearData = async () => {
    confirm({
      title: "Are you absolutely sure?",
      description: `This action cannot be undone. This will permanently delete all your purchase records in ${current?.name}.`,
      onConfirm: async () => {
        await fetchWithAuth("/api/transaction/clear", {
          method: "POST",
        });
        await loadSpaces();
        await loadTransactions(true);
        toast("All your purchase records have been deleted.");
      },
    });
  };

  return (
    <div>
      <h2 className="text-2xl font-bold mb-4">Settings</h2>

      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle>Current Space</CardTitle>
            <CardDescription>
              Spaces let you separate purchases by context—like home, work, or
              family — and you can collaborate with others by sharing access.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <div className="flex items-center gap-2">
              <Label htmlFor="space-select">Current Space:</Label>
              <Select
                value={current?.id ?? ""}
                onValueChange={(value) => handleChangeSpace(value)}
              >
                <SelectTrigger className="w-[200px]" id="space-select">
                  <SelectValue placeholder="Select a space" />
                </SelectTrigger>
                <SelectContent>
                  {spaces.map((space) => (
                    <SelectItem key={space.id} value={space.id!}>
                      {space.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate("/spaces")}
            >
              Edit Spaces...
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Categories</CardTitle>
            <CardDescription>
              Add, remove or edit the categories...
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate("/categories")}
            >
              Edit Categories...
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Theme</CardTitle>
            <CardDescription>Toggle dark/light mode.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex gap-4">
              <ThemeToggle />
              <Button variant={"link"} onClick={() => setTheme("default")}>
                Use auto
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Language</CardTitle>
            <CardDescription>
              Select the language the receipts are in. It is used to recognize
              text from pictures of receipts.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <PreferredLanguage />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Account</CardTitle>
            <CardDescription>
              {name ? `${name}, you are logged in.` : "You are not logged in."}
            </CardDescription>
          </CardHeader>
          <CardContent></CardContent>
          <CardFooter>
            <Button onClick={handleLogout}>Log out</Button>
          </CardFooter>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Data Management</CardTitle>
            <CardDescription>
              Backup, restore, or clear your purchase data.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label className="text-base">Backup Data</Label>
                  <p className="text-sm text-muted-foreground">
                    Download your purchase records as a CSV or Excel file
                  </p>
                </div>
                <div className="flex flex-col gap-2">
                  <Button variant="outline" size="sm" onClick={handleBackupCsv}>
                    <Download className="h-4 w-4 mr-2" />
                    Export CSV
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleBackupExcel}
                  >
                    <Download className="h-4 w-4 mr-2" />
                    Export Excel
                  </Button>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label className="text-base">Restore Data</Label>
                  <p className="text-sm text-muted-foreground">
                    Upload a CSV or Excel file to restore your purchase records
                  </p>
                </div>
                <div>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".csv,.xlsx"
                    onChange={handleRestore}
                    className="hidden"
                    id="csv-upload"
                  />
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={isImporting}
                  >
                    {isImporting ? (
                      <>
                        <Upload className="h-4 w-4 mr-2 animate-spin" />
                        Importing...
                      </>
                    ) : (
                      <>
                        <Upload className="h-4 w-4 mr-2" />
                        Import
                      </>
                    )}
                  </Button>
                </div>
              </div>

              <Button variant="destructive" size="sm" onClick={handleClearData}>
                <Trash2 className="h-4 w-4 mr-2" />
                Clear All Data
              </Button>
            </div>
          </CardContent>
          <CardFooter>
            <div className="text-sm text-muted-foreground">
              You have {count(current?.countTransactions, "record", "records")}{" "}
              stored.
            </div>
          </CardFooter>
        </Card>

        {appVersion && (
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">
                Version: {appVersion.version}
              </p>
              <p className="text-sm text-muted-foreground">
                Commit: {appVersion.commit}
              </p>
              <p className="text-sm text-muted-foreground">
                Built: {appVersion.built}
              </p>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
