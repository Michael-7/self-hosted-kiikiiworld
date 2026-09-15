import { Link } from 'react-router-dom';
import styles from './Welcome.module.css';

interface WelcomeProps {
  onLogout: () => void;
}

export function Welcome({ onLogout }: WelcomeProps) {
  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Welcome, Michael</h1>
      <p className={styles.subtitle}>You're logged in.</p>
      <div className={styles.actions}>
        <Link to="/login/new" className={styles.newPost}>
          New post
        </Link>
        <button className={styles.logout} onClick={onLogout}>
          Log out
        </button>
      </div>
    </div>
  );
}
