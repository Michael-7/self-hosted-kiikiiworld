import { Route, Routes } from 'react-router-dom';
import styles from './App.module.css';
import { Nav } from './components/nav/Nav';
import { Posts } from './components/posts/Posts';
import { Login } from './components/login/Login';
import { NewPost } from './components/newPost/NewPost';

function Home() {
  return (
    <div className={styles.app}>
      <Nav />
      <main className={styles.main}>
        <Posts />
      </main>
    </div>
  );
}

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/login/new" element={<NewPost />} />
    </Routes>
  );
}
