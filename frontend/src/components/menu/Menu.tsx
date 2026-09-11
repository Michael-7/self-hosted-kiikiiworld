import styles from './Menu.module.css';

interface MenuProps {
  types: string[];
  selected: string | null;
  onSelect: (type: string | null) => void;
}

export function Menu({ types, selected, onSelect }: MenuProps) {
  const linkClass = (isSelected: boolean) =>
    isSelected ? `${styles.link} ${styles.selected}` : styles.link;

  return (
    <div className={styles.menu}>
      <button className={linkClass(selected === null)} onClick={() => onSelect(null)}>
        Everything
      </button>
      {types.map((type) => (
        <button key={type} className={linkClass(selected === type)} onClick={() => onSelect(type)}>
          {type}
        </button>
      ))}
    </div>
  );
}
