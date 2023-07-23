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
    mutationFn: async () =>
      await fetch(`${process.env.REACT_APP_BACKEND_URI}/settings`, {
        method: 'PUT',
        body: JSON.stringify(localSettings),
        headers: {
          'access-control-allow-origin': '*',
          'content-type': 'application/json',
        },
      }).then(value => value.json()),
    onSuccess: setGlobalSettings,
  })

  const save = () => {
    update.mutate()
    hide()
  }

  return (
    <div className='settings'>
      <Gear onClick={() => setShowModal(true)} />
      <Modal show={showModal} onHide={hide} size='lg'>
        <Modal.Header>
          <Modal.Title>Settings</Modal.Title>
          <Nav variant='tabs' defaultActiveKey={type} onSelect={setType}>
            <Nav.Item>
              <Nav.Link eventKey='default'>Basic</Nav.Link>
            </Nav.Item>
            <Nav.Item>
              <Nav.Link eventKey='advanced'>Advanced</Nav.Link>
            </Nav.Item>
          </Nav>
        </Modal.Header>
        <Modal.Body>
          <Switch test={type}>
            <Case value={'default'}>
              <Basic setSettings={setLocalSettings} settings={localSettings} />
            </Case>
            <Case value={'advanced'}>
              <Advanced setSettings={setLocalSettings} settings={localSettings} />
            </Case>
          </Switch>
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
