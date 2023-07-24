import React, { useRef, useEffect } from 'react'

// https://github.com/AndrewRedican/react-json-editor-ajrm#readme
// Hacking implementation to obtain desired behavior

import JSONInput from 'react-json-editor-ajrm'
import locale from 'react-json-editor-ajrm/locale/en'

export default function Advanced({ settings, setSettings }) {
  const ref = useRef()

  useEffect(() => {
    if (ref.current)
      ref.current.onKeyPress = () => {
        setTimeout(() => {
          const current = ref.current
          if (current) {
            const container = current.refContent
            const data = current.tokenize(container)?.jsObject
            if (data) setSettings(data)
          }
        }, 5)
      }
  }, [setSettings])

  return (
    <JSONInput
      ref={ref}
      className='advanced-settings'
      placeholder={settings}
      locale={locale}
      height='auto'
      width='auto'
      confirmGood={false}
      colors={{ background: '#191b1e' }}
      onKeyPressUpdate={false}
    />
  )
}
