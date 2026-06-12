interface IconProps {
  name: string
  filled?: boolean
  className?: string
}

export function Icon({ name, filled, className = '' }: IconProps) {
  return (
    <span className={`material-symbols-outlined select-none ${filled ? 'filled' : ''} ${className}`}>
      {name}
    </span>
  )
}
