'use client';
import { useState } from 'react';
import { useLanguage } from '@/components/localization';
export function SchedulePicker({ pickupValue = '', dropoffValue = '' }: { pickupValue?: string; dropoffValue?: string }) {
  const { t } = useLanguage();
  const [method, setMethod] = useState<'pickup' | 'dropoff'>(dropoffValue && !pickupValue ? 'dropoff' : 'pickup');
  return <fieldset className="schedule-picker"><legend>{t('လက်ခံမည့်ပုံစံ')} *</legend><div className="schedule-options"><label className={method === 'pickup' ? 'active' : ''}><input type="radio" checked={method === 'pickup'} onChange={() => setMethod('pickup')} />{t('လာယူရမည်')}</label><label className={method === 'dropoff' ? 'active' : ''}><input type="radio" checked={method === 'dropoff'} onChange={() => setMethod('dropoff')} />{t('ပို့ဆောင်ပေးမည်')}</label></div><label htmlFor="schedule-time">{t('ရက်စွဲနှင့်အချိန်')} *</label><input key={method} id="schedule-time" className="date-time-picker" type="datetime-local" name={method === 'pickup' ? 'pickupTime' : 'dropoffTime'} defaultValue={method === 'pickup' ? pickupValue : dropoffValue} required /></fieldset>;
}
