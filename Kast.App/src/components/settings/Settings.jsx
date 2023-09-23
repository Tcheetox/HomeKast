import React, { useState, useEffect } from 'react'

import './settings.scoped.scss'
import { useMutation } from '@tanstack/react-query'
import { Gear } from '../../assets/icons/'
import { Case, Switch } from '../../hoc/'
import { useSettings } from '../../hooks/'
import { useContextUpdater } from '../../AppContext'
import { Button, Nav, Offcanvas } from 'react-bootstrap'
import Basic from './Basic'
import Advanced from './Advanced'

export default function Settings() {
  const globalSettings = useSettings()
  const setGlobalSettings = useContextUpdater('settings')
  const [localSettings, setLocalSettings] = useState(globalSettings)
  const [view, setView] = useState({ show: false, mode: 'default' })

  useEffect(() => setLocalSettings(globalSettings), [globalSettings, setLocalSettings])

  const hide = () => setView(p => ({ ...p, show: false }))

  const update = useMutation({
    mutationFn: () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/settings`, {
        method: 'PUT',
        body: JSON.stringify(localSettings),
        headers: {
          'access-control-allow-origin': '*',
          'content-type': 'application/json',
        },
      }).then(async r => {
        const bodyPromise = r.json()
        if (r.ok) return bodyPromise
        throw Error(JSON.stringify(await bodyPromise))
      }),
    onSuccess: setGlobalSettings,
  })

  const save = () => {
    update.mutate()
    hide()
  }

  return (
    <div className='settings'>
      <Gear onClick={() => setView(p => ({ ...p, show: true }))} />
      <Offcanvas show={view.show} onHide={hide} placement='end' className='settings-canvas'>
        <Offcanvas.Header closeButton>
          <Nav variant='underline' defaultActiveKey={view.mode} onSelect={m => setView(p => ({ ...p, mode: m }))}>
            <Nav.Item className='nav-default'>
              <Nav.Link eventKey='default'>Basic</Nav.Link>
            </Nav.Item>
            <Nav.Item className='nav-advanced'>
              <Nav.Link eventKey='advanced'>Advanced</Nav.Link>
            </Nav.Item>
          </Nav>
        </Offcanvas.Header>
        <Offcanvas.Body className={view.mode}>
          <hr className='top separator' />
          <div className='settings-content'>
            <Switch test={view.mode}>
              <Case value={'default'}>
                <Basic setSettings={setLocalSettings} settings={localSettings} />
              </Case>
              <Case value={'advanced'}>
                <Advanced setSettings={setLocalSettings} settings={localSettings} />
              </Case>
            </Switch>
          </div>
          <div className='offcanvas-footer'>
            <hr className='bottom separator' />
            <Button variant='secondary' size='sm' onClick={hide}>
              Close
            </Button>
            <Button variant='primary' size='sm' onClick={save}>
              Save
            </Button>
          </div>
        </Offcanvas.Body>
      </Offcanvas>
    </div>
  )
}
