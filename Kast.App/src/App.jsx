import React from 'react'
import { Route, Routes } from 'react-router-dom'

import './App.scss'

import { AppContextProvider } from './AppContext'
import Footer from './components/footer/Footer'
import Header from './components/header/Header'
import Home from './components/home/Home'

export default function App() {
  return (
    <>
      <AppContextProvider>
        <Header />
        <Routes>
          <Route path='/' element={<Home />} />
        </Routes>
      </AppContextProvider>
      <Footer />
    </>
  )
}
