import React from 'react'

import './App.scss'

import { AppContextProvider } from './AppContext'
import Footer from './components/footer/Footer'
import Header from './components/header/Header'
import Home from './components/home/Home'
import DraggablePlayer from './components/player/DraggablePlayer'

export default function App() {
  return (
    <>
      <AppContextProvider>
        <DraggablePlayer />
        <Header />
        <Home />
      </AppContextProvider>
      <Footer />
    </>
  )
}
