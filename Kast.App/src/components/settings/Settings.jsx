import React, { useState, useEffect } from 'react'

import './settings.scoped.scss'
import Gear from '../../assets/icons/settings.svg'
import { Case, Switch } from '../../hoc/'
import { useSettings } from '../../hooks/'
import { useContextUpdater } from '../../AppContext'
import { Button, Modal, Nav } from 'react-bootstrap'
import Basic from './Basic'
import Advanced from './Advanced'
import { useMutation } from '@tanstack/react-query'

export default function Settings() {
  const globalSettings = useSettings()
  const setGlobalSettings = useContextUpdater('settings')
  const [localSettings, setLocalSettings] = useState(globalSettings)
  const [showModal, setShowModal] = useState(false)
  const [type, setType] = useState('advanced')

  useEffect(() => setLocalSettings(globalSettings), [globalSettings, setLocalSettings])

  const hide = () => setShowModal(false)

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
      <Gear onClick={() => setShowModal(true)} />
      <Modal show={showModal} onHide={hide} size='lg' className='settings settings-modal' centered>
        <Modal.Header>
          <Modal.Title>Settings</Modal.Title>
          <Nav variant='underline' defaultActiveKey={type} onSelect={setType}>
            <Nav.Item className='nav-default'>
              <Nav.Link eventKey='default' disabled>
                Basic
              </Nav.Link>
            </Nav.Item>
            <Nav.Item className='nav-advanced'>
              <Nav.Link eventKey='advanced'>Advanced</Nav.Link>
            </Nav.Item>
          </Nav>
        </Modal.Header>
        <Modal.Body>
          <hr className='separator' />
          <Switch test={type}>
            <Case value={'default'}>
              <Basic setSettings={setLocalSettings} settings={localSettings} />
            </Case>
            <Case value={'advanced'}>
              <Advanced setSettings={setLocalSettings} settings={localSettings} />
            </Case>
          </Switch>
          <hr className='separator' />
        </Modal.Body>
        <Modal.Footer>
          <Button variant='secondary' size='sm' onClick={hide}>
            Close
          </Button>
          <Button variant='primary' size='sm' onClick={save}>
            Save
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  )
}
