import React from 'react'

import Logo from '../../assets/kre.svg'
import { Spinner } from 'react-bootstrap'
import './loading.scoped.scss'

export default function Loading() {
  return (
    <div className='positionner'>
      <Spinner className='loading' animation='border' variant={'light'}>
        <Logo className='logo' />
      </Spinner>
    </div>
  )
}
