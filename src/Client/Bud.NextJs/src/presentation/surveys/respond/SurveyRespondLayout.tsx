import { Outlet } from "react-router-dom";
import { BudLogo } from "@/components/BudLogo";
import styles from "./SurveyRespondLayout.module.css";

/**
 * Standalone layout for the survey respondent experience.
 * No sidebar, no navigation — just the Bud logo and the form content.
 */
export function SurveyRespondLayout() {
  return (
    <div className={styles.layout}>
      <header className={styles.header}>
        <BudLogo height={28} />
      </header>
      <main className={styles.main}>
        <Outlet />
      </main>
      <footer className={styles.footer}>
        <span className={styles.footerText}>
          Powered by <strong>Bud</strong> — Gestão de desempenho contínua
        </span>
      </footer>
    </div>
  );
}
