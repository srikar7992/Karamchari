import React from "react";
import { colors } from "../tokens/colors";

export type BadgeVariant = "success" | "warning" | "danger" | "info" | "neutral";

interface BadgeProps {
  children: React.ReactNode;
  variant?: BadgeVariant;
  className?: string;
}

/**
 * A premium badge component with glassmorphism and status-aware styling.
 */
export const Badge: React.FC<BadgeProps> = ({ 
  children, 
  variant = "neutral", 
  className = "" 
}) => {
  const getVariantStyles = () => {
    switch (variant) {
      case "success":
        return {
          backgroundColor: colors.success.bg,
          color: colors.success.text,
          borderColor: colors.success.DEFAULT,
        };
      case "warning":
        return {
          backgroundColor: colors.warning.bg,
          color: colors.warning.text,
          borderColor: colors.warning.DEFAULT,
        };
      case "danger":
        return {
          backgroundColor: colors.danger.bg,
          color: colors.danger.text,
          borderColor: colors.danger.DEFAULT,
        };
      case "info":
        return {
          backgroundColor: colors.info.bg,
          color: colors.info.text,
          borderColor: colors.info.DEFAULT,
        };
      default:
        return {
          backgroundColor: colors.background.border,
          color: colors.text.secondary,
          borderColor: colors.background.border,
        };
    }
  };

  const styles = getVariantStyles();

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border backdrop-blur-sm transition-all duration-200 ${className}`}
      style={{
        backgroundColor: styles.backgroundColor,
        color: styles.color as string,
        borderColor: styles.borderColor as string,
        boxShadow: `0 0 10px ${styles.backgroundColor}`,
      }}
    >
      {children}
    </span>
  );
};
