import styles from './App.module.css';
import { Nav } from './components/nav/Nav';
import { Posts } from './components/posts/Posts';

export function App() {
  return (
    <div className={styles.app}>
      <Nav />
      <main className={styles.main}>
        <Posts />
      </main>
    </div>
  );
}
